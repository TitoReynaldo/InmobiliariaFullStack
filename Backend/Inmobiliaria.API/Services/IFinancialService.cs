using System.Threading.Tasks;
using Inmobiliaria.API.DTOs.Simulacion;

namespace Inmobiliaria.API.Services
{
    public interface IFinancialService
    {
        Task<SimulacionResultDto> CalcularSimulacion(SimulacionInputDto input);
    }
}
