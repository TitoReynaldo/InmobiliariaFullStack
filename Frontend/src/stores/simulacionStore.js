import { ref } from 'vue'
import { defineStore } from 'pinia'
import { SimulacionService } from '../services/simulacionService'

export const useSimulacionStore = defineStore('simulacion', () => {
  const input = ref({
    direccionPropiedad: '',
    moneda: 'PEN',
    precioVivienda: 250000,
    valorTasacion: 250000,
    cuotaInicialPorcentaje: 20,
    diasPorPeriodo: 30,
    diasPorAnio: 360,
    tasaInteres: 10.8,
    tipoTasa: 'Efectiva',
    plazoMeses: 240,
    tipoGracia: 'Sin Gracia',
    periodosGracia: 0,
    aplicaBonoBuenPagador: false,
    aplicaBonoVerde: false,
    aplicaTechoPropio: false,

    costesNotariales: 150,
    costesRegistrales: 250,
    tasacion: 300,

    portes: 3.5,
    gastosAdministracion: 10,
    seguroDesgravamenMensual: 0.049,
    seguroRiesgoMensual: 0.029,

    mesesPorCuota: 1,
    incrementoTasaFutura: 0,
    cuotaInicioAjuste: 0,
    tipoPrepago: 'ReducirCuota',
    pagosAnticipados: {},

    tasaDescuento: 12.5,
    ingresoNeto: 5000,
    situacionLaboral: 'Dependiente',
    scoreEndeudamiento: 100,
    ubicacionGeografica: '',
    tipoInmueble: 'Departamento',
    areaTotal: 60,
  })

  const resultado = ref(null)
  const advertenciasRegulatorias = ref([])
  const loading = ref(false)
  const errorMsg = ref('')

  const modoLectura = ref(false)
  const modoEdicion = ref(false)
  const simulacionId = ref(null)

  const calcular = async () => {
    if (modoLectura.value) return false

    loading.value = true
    errorMsg.value = ''
    resultado.value = null
    advertenciasRegulatorias.value = []

    try {
      const payload = {
        ...input.value,
        moneda: input.value.moneda,
        precioVivienda: parseFloat(input.value.precioVivienda),
        valorTasacion: parseFloat(input.value.valorTasacion),
        cuotaInicialPorcentaje: parseFloat(input.value.cuotaInicialPorcentaje),
        cuotaInicial:
          parseFloat(input.value.precioVivienda) *
          (parseFloat(input.value.cuotaInicialPorcentaje) / 100),
        diasPorPeriodo: parseInt(input.value.diasPorPeriodo, 10),
        diasPorAnio: parseInt(input.value.diasPorAnio, 10),
        tasaInteres: parseFloat(input.value.tasaInteres),
        plazoMeses: parseInt(input.value.plazoMeses, 10),
        mesesGracia: parseInt(input.value.mesesGracia, 10),
        costesNotariales: parseFloat(input.value.costesNotariales),
        costesRegistrales: parseFloat(input.value.costesRegistrales),
        tasacion: parseFloat(input.value.tasacion),
        comisionActivacion: parseFloat(input.value.comisionActivacion),
        portes: parseFloat(input.value.portes),
        gastosAdministracion: parseFloat(input.value.gastosAdministracion),
        seguroDesgravamenMensual: parseFloat(input.value.seguroDesgravamenMensual),
        seguroRiesgoMensual: parseFloat(input.value.seguroRiesgoMensual),
        tasaDescuento: parseFloat(input.value.tasaDescuento),
      }

      console.log('Payload enviado:', payload)

      let response
      if (simulacionId.value && simulacionId.value > 0) {
        response = await SimulacionService.actualizar(simulacionId.value, payload)
      } else {
        response = await SimulacionService.calcular(payload)
      }

      resultado.value = response.data.resultado || response.data
      advertenciasRegulatorias.value = response.data.advertenciasRegulatorias || response.data.AdvertenciasRegulatorias || []
      return true
    } catch (error) {
      console.error('Error al calcular en Store:', error)
      const data = error.response?.data
      if (data) {
        errorMsg.value = [data.message, data.detalle].filter(Boolean).join(' - ')
      } else {
        errorMsg.value = 'Error al procesar la simulación.'
      }
      return false
    } finally {
      loading.value = false
    }
  }

  const cargarDesdeHistorial = (resumen) => {
    console.log('Iniciando reconstrucción modo lectura con datos del historial:', resumen)
    modoLectura.value = true
    modoEdicion.value = false
    simulacionId.value = resumen.simulacionId || resumen.SimulacionId

    _poblarInputParaEdicion(resumen)

    const detallesArray = resumen.detalles || resumen.Detalles || resumen.detalleCronogramas || resumen.DetalleCronogramas || resumen.cronograma || [];

    const calcAmortizacion = detallesArray.reduce((acc, curr) => acc + Number(curr.amortizacion || curr.Amortizacion || 0), 0);
    const calcIntereses = detallesArray.reduce((acc, curr) => acc + Number(curr.interes || curr.Interes || 0), 0);
    const calcSeguros = detallesArray.reduce((acc, curr) => acc + Number(curr.segDesgravamen || curr.SegDesgravamen || curr.seguroDesgravamen || 0) + Number(curr.segInmueble || curr.SegInmueble || curr.seguroInmueble || curr.seguroRiesgo || 0), 0);
    const calcGastos = detallesArray.reduce((acc, curr) => acc + Number(curr.portes || curr.Portes || 0) + Number(curr.gastosAdministracion || curr.GastosAdministracion || 0), 0);

    resultado.value = {
      cronograma: detallesArray,
      tcea: resumen.tcea ?? resumen.Tcea ?? 0,
      tir: resumen.tir ?? resumen.Tir ?? 0,
      van: resumen.van ?? resumen.Van ?? 0,
      totalAmortizacion: calcAmortizacion,
      totalIntereses: calcIntereses,
      totalSeguros: calcSeguros,
      totalPortes: 0,
      totalGastosAdmin: calcGastos
    }
  }

  const cargarParaEdicion = (datos) => {
    modoLectura.value = false
    modoEdicion.value = true
    simulacionId.value = datos.simulacionId || datos.SimulacionId

    _poblarInputParaEdicion(datos)
    resultado.value = null
  }

  const _poblarInputParaEdicion = (datos) => {
    input.value.direccionPropiedad = datos.direccionPropiedad || datos.DireccionPropiedad || '';
    input.value.precioVivienda = Number(datos.precioVenta ?? datos.PrecioVenta ?? datos.tasacionActivo ?? datos.TasacionActivo ?? input.value.precioVivienda);
    input.value.plazoMeses = Number(datos.plazo ?? datos.Plazo ?? datos.plazoMeses ?? datos.PlazoMeses ?? input.value.plazoMeses);
    input.value.moneda = datos.moneda ?? datos.Moneda ?? input.value.moneda;
    input.value.valorTasacion = Number(datos.valorTasacion ?? datos.ValorTasacion ?? input.value.precioVivienda);
    input.value.cuotaInicial = Number(datos.cuotaInicial ?? datos.CuotaInicial ?? 0);

    input.value.tasaInteres = Number(datos.tasaEfectivaAnual ?? datos.TasaEfectivaAnual ?? datos.tasaInteres ?? datos.TasaInteres ?? input.value.tasaInteres);
    input.value.tipoTasa = datos.tipoTasa ?? datos.TipoTasa ?? 'Efectiva';
    input.value.diasPorPeriodo = Number(datos.diasPorPeriodo ?? datos.DiasPorPeriodo ?? 30);
    input.value.diasPorAnio = Number(datos.diasPorAnio ?? datos.DiasPorAnio ?? 360);
    input.value.tipoGracia = datos.tipoGracia ?? datos.TipoGracia ?? 'Sin Gracia';
    input.value.periodosGracia = Number(datos.mesesGracia ?? datos.MesesGracia ?? datos.periodosGracia ?? 0);

    input.value.seguroDesgravamenMensual = Number(datos.tasaSeguroDesgravamen ?? datos.TasaSeguroDesgravamen ?? datos.seguroDesgravamenMensual ?? input.value.seguroDesgravamenMensual);
    input.value.seguroRiesgoMensual = Number(datos.tasaSeguroInmueble ?? datos.TasaSeguroInmueble ?? datos.seguroRiesgoMensual ?? input.value.seguroRiesgoMensual);
    input.value.portes = Number(datos.portes ?? datos.Portes ?? input.value.portes);
    input.value.gastosAdministracion = Number(datos.gastosAdministracion ?? datos.GastosAdministracion ?? input.value.gastosAdministracion);

    input.value.costesNotariales = Number(datos.costesNotariales ?? datos.CostesNotariales ?? input.value.costesNotariales);
    input.value.costesRegistrales = Number(datos.costesRegistrales ?? datos.CostesRegistrales ?? input.value.costesRegistrales);
    input.value.tasacion = Number(datos.tasacion ?? datos.Tasacion ?? input.value.tasacion);

    input.value.aplicaBonoVerde = Boolean(datos.aplicaBonoVerde || datos.AplicaBonoVerde || false);
    input.value.aplicaBonoBuenPagador = Boolean(datos.flagBFH || datos.FlagBFH || false);

    input.value.incrementoTasaFutura = Number(datos.incrementoTasaFutura ?? datos.IncrementoTasaFutura ?? 0);
    input.value.cuotaInicioAjuste = Number(datos.cuotaInicioAjuste ?? datos.CuotaInicioAjuste ?? 0);
    input.value.tipoPrepago = datos.tipoPrepago ?? datos.TipoPrepago ?? 'ReducirCuota';
    input.value.tasaDescuento = Number(datos.tasaDescuento ?? datos.TasaDescuento ?? 12.5);

    const precio = Number(datos.precioVenta ?? datos.PrecioVenta ?? input.value.precioVivienda);
    const prestamo = Number(datos.montoPrestamo ?? datos.MontoPrestamo ?? (precio - input.value.cuotaInicial));
    if (precio > 0 && prestamo > 0 && prestamo <= precio) {
      input.value.cuotaInicialPorcentaje = Number((((precio - prestamo) / precio) * 100).toFixed(2));
    }
  }

  const habilitarEdicion = () => {
    modoLectura.value = false;
    modoEdicion.value = true;
  }

  const resetearResultados = () => {
    resultado.value = null
    errorMsg.value = ''
    modoLectura.value = false
    modoEdicion.value = false
    simulacionId.value = null

    input.value = {
      direccionPropiedad: '',
      moneda: 'PEN',
      precioVivienda: 250000,
      valorTasacion: 250000,
      cuotaInicialPorcentaje: 20,
      diasPorPeriodo: 30,
      diasPorAnio: 360,
      tasaInteres: 10.8,
      tipoTasa: 'Efectiva',
      plazoMeses: 240,
      tipoGracia: 'Sin Gracia',
      periodosGracia: 0,
      aplicaBonoBuenPagador: false,
      aplicaBonoVerde: false,
      aplicaTechoPropio: false,
      costesNotariales: 150,
      costesRegistrales: 250,
      tasacion: 300,
      portes: 3.5,
      gastosAdministracion: 10,
      seguroDesgravamenMensual: 0.049,
      seguroRiesgoMensual: 0.029,
      mesesPorCuota: 1,
      incrementoTasaFutura: 0,
      cuotaInicioAjuste: 0,
      tipoPrepago: 'ReducirCuota',
      pagosAnticipados: {},
      tasaDescuento: 12.5,
      ingresoNeto: 5000,
      situacionLaboral: 'Dependiente',
      scoreEndeudamiento: 100,
      ubicacionGeografica: '',
      tipoInmueble: 'Departamento',
      areaTotal: 60,
    }
  }

  return {
    input,
    resultado,
    advertenciasRegulatorias,
    loading,
    errorMsg,
    modoLectura,
    modoEdicion,
    simulacionId,
    calcular,
    cargarDesdeHistorial,
    cargarParaEdicion,
    habilitarEdicion,
    resetearResultados,
  }
})