using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Inmobiliaria.API.Models;
using Inmobiliaria.API.DTOs.Simulacion;
using Inmobiliaria.API.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Inmobiliaria.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SimulacionesController : ControllerBase
    {
        private readonly InmobiliariaContext _context;
        private readonly IFinancialService _financialService;
        private readonly ILogger<SimulacionesController> _logger;

        public SimulacionesController(InmobiliariaContext context, IFinancialService financialService, ILogger<SimulacionesController> logger)
        {
            _context = context;
            _financialService = financialService;
            _logger = logger;
        }

        [HttpPost("calcular")]
        public async Task<IActionResult> CalcularYGuardarSimulacion([FromBody] SimulacionInputDto input)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int usuarioId)) return Unauthorized();

                var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
                if (cliente == null)
                {
                    cliente = new Cliente { UsuarioId = usuarioId, Dni = usuarioId.ToString().PadLeft(8, '0'), Nombres = User.FindFirst(ClaimTypes.Name)?.Value ?? "Cliente Demo", Apellidos = "Demo", SueldoMensual = input.IngresoNeto ?? 5000m, IngresoNeto = input.IngresoNeto ?? 5000m, FechaNacimiento = new DateOnly(1990, 1, 1) };
                    _context.Clientes.Add(cliente);
                }
                else
                {
                    cliente.IngresoNeto = input.IngresoNeto ?? cliente.IngresoNeto;
                    cliente.SituacionLaboral = input.SituacionLaboral ?? cliente.SituacionLaboral;
                    _context.Clientes.Update(cliente);
                }
                await _context.SaveChangesAsync();

                var resultado = await _financialService.CalcularSimulacion(input);
                resultado.Moneda = input.Moneda;

                using var transaction = await _context.Database.BeginTransactionAsync();

                var nuevaPropiedad = new Propiedade { 
                    Direccion = input.DireccionPropiedad, 
                    PrecioVenta = input.PrecioVivienda,
                    TasacionActivo = input.PrecioVivienda, 
                    UbicacionGeografica = input.UbicacionGeografica, 
                    TipoInmueble = input.TipoInmueble, 
                    AreaTotal = input.AreaTotal,
                    EsBonoVerde = input.AplicaBonoVerde,
                    FlagBfh = input.AplicaBonoBuenPagador,
                    FlagBonoVerde = input.AplicaBonoVerde,
                    MonedaBase = input.Moneda
                };
                _context.Propiedades.Add(nuevaPropiedad);
                await _context.SaveChangesAsync();

                var nuevaConfig = new Configfinanciera {
                    Moneda = input.Moneda,
                    CuotaInicial = input.CuotaInicial > 0 ? input.CuotaInicial : (input.PrecioVivienda * (input.CuotaInicialPorcentaje / 100m)),
                    PlazoMeses = input.PlazoMeses > 0 ? input.PlazoMeses : input.PlazoAnios * 12,
                    TipoTasa = input.TipoTasa,
                    TasaInteres = input.TasaInteres,
                    DiasPorPeriodo = (input.DiasPorPeriodo > 0 ? input.DiasPorPeriodo : 30) * (input.MesesPorCuota > 0 ? input.MesesPorCuota : 1),
                    DiasPorAnio = input.DiasPorAnio,
                    TipoGracia = input.TipoGracia,
                    MesesGracia = input.PeriodosGracia,
                    CostesNotariales = input.CostesNotariales,
                    CostesRegistrales = input.CostesRegistrales,
                    Tasacion = input.Tasacion,
                    Portes = input.Portes,
                    GastosAdministracion = input.GastosAdministracion,
                    PorcentajeDesgravamen = input.SeguroDesgravamenMensual,
                    PorcentajeSeguroInmueble = input.SeguroRiesgoMensual,
                    TasaDescuento = input.TasaDescuento,
                    PrepagosJson = input.PagosAnticipados != null ? JsonSerializer.Serialize(input.PagosAnticipados) : null
                };
                _context.Configfinancieras.Add(nuevaConfig);
                await _context.SaveChangesAsync();

                var simulacionDb = new Simulacione
                {
                    ClienteId = cliente.ClienteId,
                    PropiedadId = nuevaPropiedad.PropiedadId,
                    ConfigId = nuevaConfig.ConfigId,
                    MontoPrestamo = (decimal)(input.PrecioVivienda - (input.CuotaInicial > 0 ? input.CuotaInicial : (input.PrecioVivienda * (input.CuotaInicialPorcentaje / 100m)))),
                    TasaEfectivaAnual = resultado.TEA,
                    Van = resultado.VAN,
                    Tir = resultado.TIR,
                    Tcea = resultado.TCEA,
                    FechaSimulacion = DateTime.Now
                };

                _context.Simulaciones.Add(simulacionDb);
                await _context.SaveChangesAsync();

                var detallesDb = new List<Detallecronograma>();
                foreach (var d in resultado.Cronograma)
                {
                    detallesDb.Add(new Detallecronograma { 
                        SimulacionId = simulacionDb.SimulacionId, 
                        NroCuota = d.NroCuota, 
                        TasaEfectivaPeriodo = d.TasaPeriodo, 
                        SaldoInicial = d.SaldoInicial, 
                        Interes = d.Interes, 
                        Amortizacion = d.Amortizacion, 
                        Cuota = d.Cuota, 
                        SeguroDesgravamen = d.SegDesgravamen, 
                        SeguroInmueble = d.SeguroRiesgo, 
                        CuotaTotal = d.CuotaTotal, 
                        SaldoFinal = d.SaldoFinal 
                    });
                }

                _context.Detallecronogramas.AddRange(detallesDb);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { resultado = resultado, AdvertenciasRegulatorias = new List<string>() });
            }
            catch (Exception ex) { return StatusCode(500, new { message = "Error interno del servidor.", detalle = ex.Message }); }
        }

        [HttpGet("historial")]
        public async Task<IActionResult> ObtenerHistorial()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int usuarioId)) return Unauthorized();

            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
            if (cliente == null) return Ok(new List<object>());

            var simulacionesDb = await _context.Simulaciones
                .Include(s => s.Propiedad)
                .Include(s => s.Config)
                .Include(s => s.Cliente)
                .Include(s => s.Detallecronogramas)
                .Where(s => s.ClienteId == cliente.ClienteId)
                .OrderByDescending(s => s.FechaSimulacion)
                .ToListAsync(); // Extracción forzada a RAM

            var historial = simulacionesDb.Select(s => new
            {
                s.SimulacionId,
                DireccionPropiedad = s.Propiedad != null ? s.Propiedad.Direccion : "Sin Nombre",
                PrecioVivienda = s.Propiedad != null ? s.Propiedad.PrecioVenta : 0m,
                ValorTasacion = s.Propiedad != null ? s.Propiedad.TasacionActivo : 0m,
                s.FechaSimulacion,
                s.MontoPrestamo,
                s.Tcea,
                s.Van,
                s.Tir,
                PlazoMeses = s.Config != null ? s.Config.PlazoMeses : 0,
                Moneda = s.Config != null ? s.Config.Moneda : "PEN",
                CuotaInicial = s.Config != null ? s.Config.CuotaInicial : 0m,
                CuotaInicialPorcentaje = (s.Propiedad != null && s.Propiedad.PrecioVenta > 0 && s.Config != null) ? Math.Round((s.Config.CuotaInicial / s.Propiedad.PrecioVenta) * 100, 2) : 0m,
                IngresoNeto = s.Cliente != null ? s.Cliente.IngresoNeto : 0m,
                SituacionLaboral = s.Cliente != null ? s.Cliente.SituacionLaboral : "Dependiente",
                TipoInmueble = s.Propiedad != null ? s.Propiedad.TipoInmueble : "Departamento",
                AreaTotal = s.Propiedad != null ? s.Propiedad.AreaTotal : 0m,
                TasaInteres = s.Config != null ? s.Config.TasaInteres : 0m,
                TipoTasa = s.Config != null ? s.Config.TipoTasa : "Efectiva",
                DiasPorPeriodo = s.Config != null ? (s.Config.DiasPorPeriodo ?? 30) / ((s.Config.DiasPorPeriodo ?? 30) >= 30 ? (s.Config.DiasPorPeriodo.Value / 30) : 1) : 30,
                DiasPorAnio = s.Config != null ? s.Config.DiasPorAnio : 360,
                MesesPorCuota = s.Config != null ? ((s.Config.DiasPorPeriodo ?? 30) >= 30 ? (s.Config.DiasPorPeriodo.Value / 30) : 1) : 1,
                TipoGracia = s.Config != null ? s.Config.TipoGracia : "Sin Gracia",
                PeriodosGracia = s.Config != null ? s.Config.MesesGracia : 0,
                CostesNotariales = s.Config != null ? s.Config.CostesNotariales : 0m,
                CostesRegistrales = s.Config != null ? s.Config.CostesRegistrales : 0m,
                Tasacion = s.Config != null ? s.Config.Tasacion : 0m,
                GastosAdministracion = s.Config != null ? s.Config.GastosAdministracion : 0m,
                Portes = s.Config != null ? s.Config.Portes : 0m, 
                SeguroDesgravamenMensual = s.Config != null ? s.Config.PorcentajeDesgravamen : 0m,
                SeguroRiesgoMensual = s.Config != null ? s.Config.PorcentajeSeguroInmueble : 0m,
                TasaDescuento = s.Config != null ? s.Config.TasaDescuento : 12.5m,
                AplicaBonoBuenPagador = s.Propiedad != null && s.Propiedad.FlagBfh.HasValue ? s.Propiedad.FlagBfh.Value : false,
                AplicaBonoVerde = s.Propiedad != null && s.Propiedad.EsBonoVerde.HasValue ? s.Propiedad.EsBonoVerde.Value : false,
                PagosAnticipados = s.Config != null && !string.IsNullOrEmpty(s.Config.PrepagosJson) ? JsonSerializer.Deserialize<Dictionary<int, decimal>>(s.Config.PrepagosJson, (JsonSerializerOptions?)null) : new Dictionary<int, decimal>(),
                Detalles = s.Detallecronogramas.OrderBy(d => d.NroCuota).Select(d => new { d.NroCuota, TasaPeriodo = d.TasaEfectivaPeriodo, SaldoInicial = d.SaldoInicial, Interes = d.Interes, Amortizacion = d.Amortizacion, Cuota = d.Cuota, SegDesgravamen = d.SeguroDesgravamen, SeguroRiesgo = d.SeguroInmueble, CuotaTotal = d.CuotaTotal, SaldoFinal = d.SaldoFinal }).ToList()
            }).ToList();

            return Ok(historial);
        }

        [HttpPut("actualizar/{id}")]
        public async Task<IActionResult> ActualizarSimulacion(int id, [FromBody] SimulacionInputDto input)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int usuarioId)) return Unauthorized();

                var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
                if (cliente == null) return NotFound("Cliente no encontrado.");

                cliente.IngresoNeto = input.IngresoNeto ?? cliente.IngresoNeto;
                cliente.SituacionLaboral = input.SituacionLaboral ?? cliente.SituacionLaboral;
                _context.Clientes.Update(cliente);

                var simulacionDb = await _context.Simulaciones
                    .Include(s => s.Propiedad)
                    .Include(s => s.Config)
                    .Include(s => s.Detallecronogramas)
                    .FirstOrDefaultAsync(s => s.SimulacionId == id && s.ClienteId == cliente.ClienteId);

                if (simulacionDb == null) return NotFound("Simulación no encontrada.");

                var resultado = await _financialService.CalcularSimulacion(input);
                resultado.Moneda = input.Moneda;

                using var transaction = await _context.Database.BeginTransactionAsync();

                if (simulacionDb.Propiedad != null)
                {
                    simulacionDb.Propiedad.Direccion = input.DireccionPropiedad;
                    simulacionDb.Propiedad.PrecioVenta = input.PrecioVivienda;
                    simulacionDb.Propiedad.TasacionActivo = input.PrecioVivienda;
                    simulacionDb.Propiedad.EsBonoVerde = input.AplicaBonoVerde;
                    simulacionDb.Propiedad.FlagBfh = input.AplicaBonoBuenPagador;
                    simulacionDb.Propiedad.FlagBonoVerde = input.AplicaBonoVerde;
                    simulacionDb.Propiedad.MonedaBase = input.Moneda;
                }

                if (simulacionDb.Config != null)
                {
                    simulacionDb.Config.Moneda = input.Moneda;
                    simulacionDb.Config.CuotaInicial = input.CuotaInicial > 0 ? input.CuotaInicial : (input.PrecioVivienda * (input.CuotaInicialPorcentaje / 100m));
                    simulacionDb.Config.PlazoMeses = input.PlazoMeses > 0 ? input.PlazoMeses : input.PlazoAnios * 12;
                    simulacionDb.Config.TipoTasa = input.TipoTasa;
                    simulacionDb.Config.TasaInteres = input.TasaInteres;
                    simulacionDb.Config.DiasPorPeriodo = (input.DiasPorPeriodo > 0 ? input.DiasPorPeriodo : 30) * (input.MesesPorCuota > 0 ? input.MesesPorCuota : 1);
                    simulacionDb.Config.DiasPorAnio = input.DiasPorAnio;
                    simulacionDb.Config.TipoGracia = input.TipoGracia;
                    simulacionDb.Config.MesesGracia = input.PeriodosGracia;
                    simulacionDb.Config.CostesNotariales = input.CostesNotariales;
                    simulacionDb.Config.CostesRegistrales = input.CostesRegistrales;
                    simulacionDb.Config.Tasacion = input.Tasacion;
                    simulacionDb.Config.Portes = input.Portes;
                    simulacionDb.Config.GastosAdministracion = input.GastosAdministracion;
                    simulacionDb.Config.PorcentajeDesgravamen = input.SeguroDesgravamenMensual;
                    simulacionDb.Config.PorcentajeSeguroInmueble = input.SeguroRiesgoMensual;
                    simulacionDb.Config.TasaDescuento = input.TasaDescuento;
                    simulacionDb.Config.PrepagosJson = input.PagosAnticipados != null ? JsonSerializer.Serialize(input.PagosAnticipados) : null;
                }

                simulacionDb.MontoPrestamo = (decimal)(input.PrecioVivienda - (input.CuotaInicial > 0 ? input.CuotaInicial : (input.PrecioVivienda * (input.CuotaInicialPorcentaje / 100m))));
                simulacionDb.TasaEfectivaAnual = resultado.TEA;
                simulacionDb.Van = resultado.VAN;
                simulacionDb.Tir = resultado.TIR;
                simulacionDb.Tcea = resultado.TCEA;
                simulacionDb.FechaSimulacion = DateTime.Now;

                _context.Detallecronogramas.RemoveRange(simulacionDb.Detallecronogramas);
                await _context.SaveChangesAsync();

                var detallesDb = new List<Detallecronograma>();
                foreach (var d in resultado.Cronograma)
                {
                    detallesDb.Add(new Detallecronograma { 
                        SimulacionId = simulacionDb.SimulacionId, 
                        NroCuota = d.NroCuota, 
                        TasaEfectivaPeriodo = d.TasaPeriodo, 
                        SaldoInicial = d.SaldoInicial, 
                        Interes = d.Interes, 
                        Amortizacion = d.Amortizacion, 
                        Cuota = d.Cuota, 
                        SeguroDesgravamen = d.SegDesgravamen, 
                        SeguroInmueble = d.SeguroRiesgo, 
                        CuotaTotal = d.CuotaTotal, 
                        SaldoFinal = d.SaldoFinal 
                    });
                }

                _context.Detallecronogramas.AddRange(detallesDb);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { resultado = resultado, AdvertenciasRegulatorias = new List<string>() });
            }
            catch (Exception ex) 
            { 
                return StatusCode(500, new { message = "Error interno del servidor.", detalle = ex.Message }); 
            }
        }

        [HttpDelete("eliminar/{id}")]
        public async Task<IActionResult> EliminarSimulacion(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int usuarioId)) return Unauthorized();

            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
            if (cliente == null) return NotFound();

            var simulacion = await _context.Simulaciones.FindAsync(id);
            if (simulacion == null || simulacion.ClienteId != cliente.ClienteId) return NotFound();

            var detalles = _context.Detallecronogramas.Where(d => d.SimulacionId == id);
            _context.Detallecronogramas.RemoveRange(detalles);
            
            var config = await _context.Configfinancieras.FindAsync(simulacion.ConfigId);
            if (config != null) _context.Configfinancieras.Remove(config);
            
            var prop = await _context.Propiedades.FindAsync(simulacion.PropiedadId);
            if (prop != null) _context.Propiedades.Remove(prop);
            
            _context.Simulaciones.Remove(simulacion);

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}