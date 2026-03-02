import api from './api'

export const SimulacionService = {
  calcular(datos) {
    return api.post('/simulaciones/calcular', datos)
  },

  obtenerHistorial() {
    return api.get('/simulaciones/historial')
  },

  eliminarSimulacion(id) {
    return api.delete(`/simulaciones/eliminar/${id}`)
  }
}
