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
            if (!ModelState.IsValid)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("\n[ERROR DE VALIDACIÓN DTO] " + JsonSerializer.Serialize(ModelState));
                Console.ResetColor();
                return BadRequest(ModelState);
            }

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int usuarioId))
                {
                    return Unauthorized("Token inválido o expirado.");
                }

                var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

                if (cliente == null)
                {
                    cliente = new Cliente
                    {
                        UsuarioId = usuarioId,
                        Dni = usuarioId.ToString().PadLeft(8, '0'),
                        Nombres = User.FindFirst(ClaimTypes.Name)?.Value ?? "Cliente Demo",
                        Apellidos = "Demo",
                        SueldoMensual = input.IngresoNeto ?? 5000m,
                        IngresoNeto = input.IngresoNeto ?? 5000m,
                        SituacionLaboral = input.SituacionLaboral,
                        ScoreEndeudamiento = input.ScoreEndeudamiento,
                        FechaNacimiento = new DateOnly(1990, 1, 1)
                    };
                    _context.Clientes.Add(cliente);
                    await _context.SaveChangesAsync();
                }

                var resultado = await _financialService.CalcularSimulacion(input);
                resultado.Moneda = input.Moneda;

                using var transaction = await _context.Database.BeginTransactionAsync();
                
                var nuevaPropiedad = new Propiedade
                {
                    Direccion = input.DireccionPropiedad,
                    TasacionActivo = input.PrecioVivienda,
                    UbicacionGeografica = input.UbicacionGeografica,
                    TipoInmueble = input.TipoInmueble,
                    AreaTotal = input.AreaTotal
                };
                _context.Propiedades.Add(nuevaPropiedad);
                await _context.SaveChangesAsync();

                var configDefecto = await _context.Configfinancieras.FirstOrDefaultAsync();
                if (configDefecto == null)
                {
                    configDefecto = new Configfinanciera
                    {
                        NombreBanco = "Banco Continental (Auto-Seed)",
                        TipoTasa = "Efectiva",
                        TasaInteres = 0.125M,
                        PorcentajeDesgravamen = 0.0005M,
                        PorcentajeSeguroInmueble = 0.0002M
                    };
                    _context.Configfinancieras.Add(configDefecto);
                    await _context.SaveChangesAsync();
                }


                var simulacionDb = new Simulacione
                {
                    ClienteId = cliente.ClienteId,
                    PropiedadId = nuevaPropiedad.PropiedadId,
                    ConfigId = configDefecto.ConfigId,
                    CuotaInicial = input.CuotaInicial,
                    MontoPrestamo = input.PrecioVivienda - input.CuotaInicial,
                    PlazoMeses = input.PlazoMeses > 0 ? input.PlazoMeses : input.PlazoAnios * 12,
                    TipoGracia = input.TipoGracia,
                    MesesGracia = input.PeriodosGracia,
                    TasaEfectivaAnual = resultado.TEA,
                    Tcea = resultado.TCEA,
                    Van = resultado.VAN,
                    Tir = resultado.TIR,
                    FechaSimulacion = DateTime.Now,
                    Moneda = input.Moneda
                };

                _context.Simulaciones.Add(simulacionDb);
                await _context.SaveChangesAsync();

                var advertencias = new List<string>();

                if (nuevaPropiedad.TasacionActivo > 343900M)
                {
                    nuevaPropiedad.FlagBfh = false;
                    advertencias.Add("ALERTA VIVIENDA: Tasación excede tope excepcional RM 421-2022. Se deshabilita BFH.");
                }

                var detallesDb = new List<Detallecronograma>();
                decimal tasaSeguroDesgravamen = simulacionDb.TasaSeguroDesgravamen ?? (configDefecto.PorcentajeDesgravamen / 100M);

                foreach (var d in resultado.Cronograma)
                {
                    
                    if (d.CuotaTotal > ((cliente.IngresoNeto ?? 0m) * 0.30m))
                    {
                        if (!advertencias.Contains("ALERTA SBS: Cuota excede el 30% del ingreso neto."))
                        {
                            advertencias.Add("ALERTA SBS: Cuota excede el 30% del ingreso neto.");
                        }
                    }

                    detallesDb.Add(new Detallecronograma
                    {
                        SimulacionId = simulacionDb.SimulacionId,
                        NroCuota = d.NroCuota,
                        TasaEfectivaPeriodo = d.TasaPeriodo,
                        SaldoInicial = d.SaldoInicial,
                        Interes = d.Interes,
                        Amortizacion = d.Amortizacion,
                        Cuota = d.Amortizacion + d.Interes,
                        SeguroDesgravamen = d.SegDesgravamen,
                        SeguroInmueble = d.SeguroRiesgo,
                        CuotaTotal = d.CuotaTotal,
                        SaldoFinal = d.SaldoFinal,
                    });
                }

                _context.Detallecronogramas.AddRange(detallesDb);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new { resultado = resultado, AdvertenciasRegulatorias = advertencias });
            }
            catch (InvalidOperationException ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n[ADVERTENCIA DE NEGOCIO] {ex.Message}\nPayload: {JsonSerializer.Serialize(input)}\n");
                Console.ResetColor();
                return BadRequest(new { message = "Inconsistencia matemática en la simulación", detalle = ex.Message });
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ERROR CRÍTICO] {ex.Message}\nStackTrace: {ex.StackTrace}\nPayload: {JsonSerializer.Serialize(input)}\n");
                Console.ResetColor();
                return StatusCode(500, new { message = "Error interno del servidor.", detalle = ex.Message });
            }
        }

        [HttpGet("historial")]
        public async Task<IActionResult> ObtenerHistorial()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int usuarioId))
            {
                return Unauthorized("Token inválido.");
            }
            //TRAS
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
            if (cliente == null)
            {
                return Ok(new List<object>());
            }

            var historial = await _context.Simulaciones
                .Include(s => s.Propiedad)
                .Include(s => s.Config)
                .Where(s => s.ClienteId == cliente.ClienteId)
                .OrderByDescending(s => s.FechaSimulacion)
                .Select(s => new
                {
                    s.SimulacionId,
                    DireccionPropiedad = s.Propiedad != null ? s.Propiedad.Direccion : "Sin Nombre",
                    s.FechaSimulacion,
                    s.MontoPrestamo,
                    s.Tcea,
                    s.Van,
                    s.Tir,
                    Plazo = s.PlazoMeses,
                    Moneda = s.Moneda ?? "PEN",
                    CuotaInicial = s.CuotaInicial,
                    TasaInteres = s.Config != null ? s.Config.TasaInteres : 0m,
                    TipoTasa = s.Config != null ? (s.Config.TipoTasa ?? "Efectiva") : "Efectiva",
                    Frecuencia = "Mensual",
                    DiasPorPeriodo = 30,
                    DiasPorAnio = 360,
                    TipoGracia = s.TipoGracia ?? "Sin Gracia",
                    PeriodosGracia = s.MesesGracia ?? 0,
                    CostesNotariales = 0m,
                    CostesRegistrales = 0m,
                    Tasacion = 0m,
                    Portes = 0m,
                    GastosAdministracion = 0m,
                    SeguroDesgravamenMensual = s.Config != null ? s.Config.PorcentajeDesgravamen : 0m,
                    SeguroRiesgoMensual = s.Config != null ? s.Config.PorcentajeSeguroInmueble : 0m,
                    IncrementoTasaFutura = 0m,
                    CuotaInicioAjuste = 0,
                    TipoPrepago = "ReducirCuota",
                    TasaDescuento = 12.5m,
                    AplicaBonoBuenPagador = false,
                    AplicaBonoVerde = s.Propiedad != null && s.Propiedad.EsBonoVerde.HasValue ? s.Propiedad.EsBonoVerde.Value : false,
                    AplicaTechoPropio = false
                })
                .ToListAsync();

            return Ok(historial);
        }

        [HttpDelete("eliminar/{id}")]
        public async Task<IActionResult> EliminarSimulacion(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int usuarioId))
            {
                return Unauthorized("Token inválido.");
            }

            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
            if (cliente == null)
            {
                return NotFound("Cliente no encontrado.");
            }

            var simulacion = await _context.Simulaciones.FindAsync(id);
            if (simulacion == null || simulacion.ClienteId != cliente.ClienteId)
            {
                return NotFound("Simulación no encontrada o acceso denegado.");
            }

            var detalles = _context.Detallecronogramas.Where(d => d.SimulacionId == id);
            _context.Detallecronogramas.RemoveRange(detalles);
            
            _context.Simulaciones.Remove(simulacion);
            
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
