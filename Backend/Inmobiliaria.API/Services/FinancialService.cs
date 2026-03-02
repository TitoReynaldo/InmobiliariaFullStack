using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inmobiliaria.API.DTOs.Simulacion;
using Inmobiliaria.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Inmobiliaria.API.Services
{
    public class FinancialService : IFinancialService
    {
        private readonly InmobiliariaContext _context;
        private const int MaxIter = 1000;
        private const double Epsilon = 1e-7;

        public FinancialService(InmobiliariaContext context)
        {
            _context = context;
        }

        public async Task<SimulacionResultDto> CalcularSimulacion(SimulacionInputDto input)
        {
            var resultado = new SimulacionResultDto();

            double montoPrestamo = (double)(input.PrecioVivienda - (input.PrecioVivienda * (input.CuotaInicialPorcentaje / 100)));

            if (input.AplicaBonoBuenPagador)
            {
                var bono = await _context.Maestrobonos
                    .FirstOrDefaultAsync(b => input.PrecioVivienda >= b.ValorViviendaMin && input.PrecioVivienda <= b.ValorViviendaMax);
                
                if (bono != null)
                {
                    montoPrestamo -= (double)bono.ValorBono;
                }
            }

            if (input.AplicaBonoVerde)
            {
         
            }

            double gastosIniciales = (double)(input.CostesNotariales + input.CostesRegistrales + input.Tasacion);

            int diasPorPeriodo = input.DiasPorPeriodo <= 0 ? 30 : input.DiasPorPeriodo;
            int diasPorAnio = input.DiasPorAnio <= 0 ? 360 : input.DiasPorAnio;
            double periodosPorAnio = (double)diasPorAnio / diasPorPeriodo;

            double tea = (double)(input.TipoTasa == "Nominal"
                ? ConvertirTasaNominalAEfectiva(input.TasaInteres / 100, periodosPorAnio)
                : input.TasaInteres / 100);

            resultado.TEA = (decimal)Math.Round(tea, 15);

            double tep = Math.Pow(1 + tea, (double)(diasPorPeriodo * input.MesesPorCuota) / diasPorAnio) - 1;

            double tasaDesgravamen = (double)(input.SeguroDesgravamenMensual / 100m);
            double montoSeguroRiesgo = (double)(input.ValorTasacion * (input.SeguroRiesgoMensual / 100m));
            double dPortes = (double)input.Portes;
            double dGastosAdmin = (double)input.GastosAdministracion;

            int mesesTotales = input.PlazoMeses > 0 ? input.PlazoMeses : input.PlazoAnios * 12;
            int totalCuotas = (int)Math.Ceiling((double)mesesTotales / input.MesesPorCuota);

            if (input.TipoGracia != "Sin Gracia" && input.PeriodosGracia >= totalCuotas)
            {
                throw new InvalidOperationException($"El periodo de gracia ({input.PeriodosGracia} periodos) no puede ser mayor o igual al plazo total del crédito ({totalCuotas} cuotas).");
            }

            int mesesGraciaReales = input.PeriodosGracia * input.MesesPorCuota;
            int limiteMeses = (input.AplicaBonoBuenPagador || input.AplicaBonoVerde || input.AplicaTechoPropio) ? 12 : 6;
            
            if (mesesGraciaReales > limiteMeses)
            {
                resultado.AdvertenciaRiesgo = $"ALERTA DE RIESGO COMERCIAL: El periodo de gracia solicitado ({mesesGraciaReales} meses) excede el límite máximo estándar permitido por políticas bancarias ({limiteMeses} meses). La simulación es matemáticamente válida, pero requiere aprobación de comité de riesgos.";
            }

            resultado.Cronograma = GenerarCronogramaFrances(
                montoPrestamo, tep,
                totalCuotas,
                input.TipoGracia, input.PeriodosGracia,
                tasaDesgravamen, montoSeguroRiesgo, dPortes, dGastosAdmin,
                input.PagosAnticipados, input.TipoPrepago,
                input.IncrementoTasaFutura, input.CuotaInicioAjuste, input.MesesPorCuota
            );

            if (resultado.Cronograma.Any()) {
                var cuotaRepresentativa = resultado.Cronograma.FirstOrDefault(c => c.Amortizacion > 0) ?? resultado.Cronograma.Last();
                resultado.CuotaMensualReferencial = Math.Round(cuotaRepresentativa.CuotaTotal - cuotaRepresentativa.SegDesgravamen - cuotaRepresentativa.SeguroRiesgo - cuotaRepresentativa.Portes - cuotaRepresentativa.GastosAdministracion, 15);
            }

            resultado.TotalAmortizacion = (decimal)Math.Round((double)resultado.Cronograma.Sum(x => x.Amortizacion), 15);
            resultado.TotalIntereses = (decimal)Math.Round((double)resultado.Cronograma.Sum(x => x.Interes), 15);
            resultado.TotalSeguros = (decimal)Math.Round((double)resultado.Cronograma.Sum(x => x.SegDesgravamen + x.SeguroRiesgo), 15);
            resultado.TotalPortes = (decimal)Math.Round((double)resultado.Cronograma.Sum(x => x.Portes), 15);
            resultado.TotalGastosAdmin = (decimal)Math.Round((double)resultado.Cronograma.Sum(x => x.GastosAdministracion), 15);

            var flujosCaja = new List<double> { -(montoPrestamo - gastosIniciales) };
            flujosCaja.AddRange(resultado.Cronograma.Select(p => (double)p.CuotaTotal));

            double tirMensual = CalcularTIRnativo(flujosCaja);
            
            if (double.IsNaN(tirMensual))
            {
                resultado.TIR = 0m;
                resultado.TCEA = 0m;
            }
            else
            {
                resultado.TIR = (decimal)Math.Round(tirMensual, 15);
                resultado.TCEA = CalcularTCEA(tirMensual, periodosPorAnio);
            }

            double cokAnual = (double)(input.TasaDescuento / 100m);
            double cokMensual = Math.Pow(1 + cokAnual, (double)diasPorPeriodo / diasPorAnio) - 1;

            resultado.VAN = (decimal)Math.Round(CalcularVANativo(cokMensual, flujosCaja), 15);

            return resultado;
        }

        private decimal ConvertirTasaNominalAEfectiva(decimal tna, double periodosPorAnio)
        {
            return (decimal)Math.Round(Math.Pow(1 + (double)tna / periodosPorAnio, periodosPorAnio) - 1, 15);
        }

        public double CalcularVANativo(double tasaDescuento, List<double> flujos)
        {
            double van = 0;
            for (int t = 0; t < flujos.Count; t++)
            {
                van += flujos[t] / Math.Pow(1 + tasaDescuento, t);
            }
            return van;
        }

        public double CalcularTIRnativo(List<double> flujos)
        {
            double tir = CalcularTIR_NewtonRaphson(flujos);
            if (double.IsNaN(tir))
            {
                tir = CalcularTIR_Biseccion(flujos);
            }
            return tir;
        }
//TRAS
        private double CalcularTIR_NewtonRaphson(List<double> flujos)
        {
            double tir = 0.01;
            for (int i = 0; i < MaxIter; i++)
            {
                double van = 0;
                double derivada = 0;
                for (int t = 0; t < flujos.Count; t++)
                { 
                    double factor = Math.Pow(1 + tir, t);
                    van += flujos[t] / factor;
                    if (t > 0)
                    {
                        derivada -= t * flujos[t] / Math.Pow(1 + tir, t + 1);
                    }
                }
                if (Math.Abs(van) < Epsilon) return tir;
                if (derivada == 0) return double.NaN;
                
                double nuevaTir = tir - van / derivada;
                if (nuevaTir <= -1.0) nuevaTir = -0.99;
                if (nuevaTir > 2.0) nuevaTir = 2.0;
                
                tir = nuevaTir;
            }
            return double.NaN;
        }

        private double CalcularTIR_Biseccion(List<double> flujos)
        {
            double low = -0.99, high = 1.0;
            double vanLow = CalcularVANativo(low, flujos);
            double vanHigh = CalcularVANativo(high, flujos);
            
            if (Math.Sign(vanLow) == Math.Sign(vanHigh)) return 0.0;

            for (int i = 0; i < MaxIter; i++)
            {
                double mid = (low + high) / 2;
                double vanMid = CalcularVANativo(mid, flujos);
                
                if (Math.Abs(vanMid) < Epsilon) return mid;
                
                if (Math.Sign(vanMid) == Math.Sign(vanLow))
                {
                    low = mid;
                    vanLow = vanMid;
                }
                else
                {
                    high = mid;
                    vanHigh = vanMid;
                }
            }
            return double.NaN;
        }

        public decimal CalcularTCEA(double tirMensual, double periodosPorAnio)
        {
            return (decimal)Math.Round(Math.Pow(1 + tirMensual, periodosPorAnio) - 1, 15);
        }

        private List<DetalleCronogramaDto> GenerarCronogramaFrances(
            double saldoCapital, double tasaMensual, int totalCuotas, string tipoGracia,
            int periodosGracia, double tasaDesgravamen, double montoSeguroRiesgo,
            double portes, double gastosAdmin, Dictionary<int, decimal>? pagosAnticipados, string tipoPrepago,
            decimal incrementoTasaFutura, int cuotaInicioAjuste, int mesesPorCuota)
        {
            var cronograma = new List<DetalleCronogramaDto>();
            double saldoInicialPeriodo = saldoCapital;

            for (int i = 1; i <= periodosGracia; i++)
            {
                double interes = saldoInicialPeriodo * tasaMensual;
                double segDesgravamen = saldoInicialPeriodo * tasaDesgravamen;
                double cuotaTotal;
                double saldoInicialActual = saldoInicialPeriodo;

                if (tipoGracia == "Total")
                {
                    saldoInicialPeriodo += interes;
                    cuotaTotal = segDesgravamen + montoSeguroRiesgo + portes + gastosAdmin;
                }
                else // Parcial
                {
                    cuotaTotal = interes + segDesgravamen + montoSeguroRiesgo + portes + gastosAdmin;
                }

                cronograma.Add(new DetalleCronogramaDto
                {
                    NroCuota = i,
                    SaldoInicial = (decimal)Math.Round(saldoInicialActual, 15),
                    Interes = (decimal)Math.Round(interes, 15),
                    Amortizacion = 0,
                    SegDesgravamen = (decimal)Math.Round(segDesgravamen, 15),
                    SeguroRiesgo = (decimal)Math.Round(montoSeguroRiesgo, 15),
                    Portes = (decimal)Math.Round(portes, 15),
                    GastosAdministracion = (decimal)Math.Round(gastosAdmin, 15),
                    CuotaTotal = (decimal)Math.Round(cuotaTotal, 15),
                    SaldoFinal = (decimal)Math.Round(saldoInicialPeriodo, 15),
                    TasaPeriodo = (decimal)Math.Round(tasaMensual, 15)
                });
            }

            int cuotasRestantes = totalCuotas - periodosGracia;
            if (cuotasRestantes > 0)
            {
                double cuotaFijaR = saldoInicialPeriodo * (Math.Pow(1 + tasaMensual, cuotasRestantes) * tasaMensual)
                                     / (Math.Pow(1 + tasaMensual, cuotasRestantes) - 1);

                for (int i = 1; i <= cuotasRestantes; i++)
                {
                    int nroCuota = i + periodosGracia;

                    if (cuotaInicioAjuste > 0 && nroCuota == cuotaInicioAjuste && incrementoTasaFutura > 0)
                    {
                        double incrementoPeriodo = (double)incrementoTasaFutura / 100.0 / 12.0 * mesesPorCuota;
                        tasaMensual += incrementoPeriodo;
                        int periodosRestantesAjuste = cuotasRestantes - i + 1;
                        if (periodosRestantesAjuste > 0)
                        {
                            cuotaFijaR = saldoInicialPeriodo * (Math.Pow(1 + tasaMensual, periodosRestantesAjuste) * tasaMensual)
                                         / (Math.Pow(1 + tasaMensual, periodosRestantesAjuste) - 1);
                        }
                    }

                    double interes = saldoInicialPeriodo * tasaMensual;
                    double amortizacion = cuotaFijaR - interes;
                    double segDesgravamen = saldoInicialPeriodo * tasaDesgravamen;

                    if (i == cuotasRestantes)
                    {
                        amortizacion = saldoInicialPeriodo;
                    }

                    double cuotaTotal = cuotaFijaR + segDesgravamen + montoSeguroRiesgo + portes + gastosAdmin;

                    double pagoExtra = 0;
                    if (pagosAnticipados != null && pagosAnticipados.ContainsKey(nroCuota))
                    {
                        pagoExtra = (double)pagosAnticipados[nroCuota];
                    }

                    amortizacion += pagoExtra;
                    cuotaTotal += pagoExtra;

                    double saldoFinal = saldoInicialPeriodo - amortizacion;
                    bool creditoTerminado = false;

                    if (saldoFinal <= 0)
                    {
                        amortizacion = saldoInicialPeriodo;
                        saldoFinal = 0;
                        cuotaTotal = amortizacion + interes + segDesgravamen + montoSeguroRiesgo + portes + gastosAdmin;
                        creditoTerminado = true;
                    }

                    cronograma.Add(new DetalleCronogramaDto
                    {
                        NroCuota = nroCuota,
                        SaldoInicial = (decimal)Math.Round(saldoInicialPeriodo, 15),
                        Interes = (decimal)Math.Round(interes, 15),
                        Amortizacion = (decimal)Math.Round(amortizacion, 15),
                        SegDesgravamen = (decimal)Math.Round(segDesgravamen, 15),
                        SeguroRiesgo = (decimal)Math.Round(montoSeguroRiesgo, 15),
                        Portes = (decimal)Math.Round(portes, 15),
                        GastosAdministracion = (decimal)Math.Round(gastosAdmin, 15),
                        CuotaTotal = (decimal)Math.Round(cuotaTotal, 15),
                        SaldoFinal = (decimal)Math.Round(saldoFinal, 15),
                        TasaPeriodo = (decimal)Math.Round(tasaMensual, 15)
                    });

                    if (creditoTerminado)
                    {
                        break;
                    }

                    saldoInicialPeriodo = saldoFinal;

                    if (saldoFinal > 0 && pagoExtra > 0 && tipoPrepago == "ReducirCuota")
                    {
                        int periodosRestantes = cuotasRestantes - i;
                        if (periodosRestantes > 0)
                        {
                            cuotaFijaR = saldoFinal * (Math.Pow(1 + tasaMensual, periodosRestantes) * tasaMensual)
                                         / (Math.Pow(1 + tasaMensual, periodosRestantes) - 1);
                        }
                    }
                }

                var ultimaCuota = cronograma.Last();
                if (ultimaCuota.SaldoFinal != 0.00m)
                {
                    if (Math.Abs(ultimaCuota.SaldoFinal) > 0.01m)
                    {
                        throw new InvalidOperationException($"Inconsistencia Matemática en Cronograma. El descuadre de {ultimaCuota.SaldoFinal:F15} supera el umbral de centavos.");
                    }
                    decimal ajuste = ultimaCuota.SaldoFinal;
                    ultimaCuota.Amortizacion += ajuste;
                    ultimaCuota.CuotaTotal += ajuste;
                    ultimaCuota.SaldoFinal = 0.00m;
                }
            }
            return cronograma;
        }
    }
}
