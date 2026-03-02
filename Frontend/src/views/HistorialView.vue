<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useSimulacionStore } from '../stores/simulacionStore'
import { SimulacionService } from '../services/simulacionService'
import jsPDF from 'jspdf'
import autoTable from 'jspdf-autotable'

const router = useRouter()
const simulacionStore = useSimulacionStore()

const historial = ref([])
const loading = ref(true)
const errorMsg = ref('')

const formatCurrency = (value, currencyCode = 'PEN') => {
  if (value === undefined || value === null) {
    return currencyCode === 'USD' ? '$ 0.00' : 'S/ 0.00'
  }
  return new Intl.NumberFormat('es-PE', {
    style: 'currency',
    currency: currencyCode,
  }).format(value)
}

const formatRate = (value) => {
  if (value === undefined || value === null) return '0.000000000000000%'
  return new Intl.NumberFormat('es-PE', {
    style: 'percent',
    minimumFractionDigits: 15,
    maximumFractionDigits: 15,
  }).format(value)
}

const formatDate = (dateString) => {
  if (!dateString) return 'Fecha no disponible'
  return new Date(dateString).toLocaleDateString('es-PE', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

const cargarHistorial = async () => {
  try {
    loading.value = true
    const { data } = await SimulacionService.obtenerHistorial()
    historial.value = data
  } catch (error) {
    console.error('Error al cargar historial:', error)
    errorMsg.value = 'No se pudo recuperar el historial de operaciones. Verifique su conexión.'
  } finally {
    loading.value = false
  }
}

const recrearSimulacion = async (resumen) => {
  try {
    await simulacionStore.cargarDesdeHistorial(resumen)
    router.push({ name: 'simulador' })
  } catch (error) {
    console.error('Error al intentar reconstruir la simulación:', error)
    alert('Ocurrió un problema al reconstruir el cronograma. Verifique la consola.')
  }
}

const editarSimulacion = (fila) => {
  simulacionStore.cargarParaEdicion(fila)
  router.push({ name: 'simulador' })
}

const confirmarEliminacion = async (id) => {
  if (window.confirm("¿Está seguro de eliminar esta simulación de forma permanente?")) {
    try {
      loading.value = true;
      await SimulacionService.eliminarSimulacion(id);
      historial.value = historial.value.filter(s => s.simulacionId !== id);
    } catch (error) {
      console.error('Error al eliminar simulación:', error);
      alert('No se pudo eliminar la simulación. Verifique su conexión o intente nuevamente.');
    } finally {
      loading.value = false;
    }
  }
}

const descargarPDF = async (simulacion) => {
  try {
    loading.value = true

    await simulacionStore.cargarDesdeHistorial(simulacion)
    const dataCompleta = simulacionStore.resultado
    const datosInput = simulacionStore.input

    if (!dataCompleta || !dataCompleta.cronograma) {
      throw new Error('No se pudo regenerar el cronograma detallado.')
    }

    const doc = new jsPDF('p', 'mm', 'a4')

    const primaryColor = [0, 53, 102]
    const secondaryColor = [234, 179, 8]
    const textColor = [60, 60, 60]
    const lightTextColor = [120, 120, 120]

    let currentY = 20

    doc.setFillColor(...primaryColor)
    doc.rect(14, currentY - 5, 16, 16, 'F')
    doc.setFontSize(9)
    doc.setTextColor(255, 255, 255)
    doc.text('BANCO', 15, currentY + 2)
    doc.text('DEMO', 15.5, currentY + 7)

    doc.setFont('helvetica', 'bold')
    doc.setFontSize(22)
    doc.setTextColor(...primaryColor)
    doc.text('BANCO HIPOTECARIO DEMO', 38, currentY + 5)

    currentY += 22
    doc.setFontSize(16)
    doc.setTextColor(...textColor)
    doc.text('Reporte Oficial de Simulación Hipotecaria', 14, currentY)

    currentY += 4
    doc.setDrawColor(...secondaryColor)
    doc.setLineWidth(0.8)
    doc.line(14, currentY, 196, currentY)

    currentY += 12
    doc.setFontSize(10)
    doc.setTextColor(...lightTextColor)

    doc.text('Detalles de la Operación:', 14, currentY)
    doc.setFont('helvetica', 'normal')
    doc.text('Propiedad:', 14, currentY + 6)
    doc.text('Fecha de Emisión:', 14, currentY + 12)

    doc.setFont('helvetica', 'bold')
    doc.setTextColor(...textColor)
    doc.text(`${simulacion.direccionPropiedad || 'Sin Nombre'}`, 50, currentY + 6)
    doc.text(
      new Date().toLocaleDateString('es-PE', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      }),
      50,
      currentY + 12,
    )

    const rightColX = 110
    doc.setFont('helvetica', 'bold')
    doc.setTextColor(...primaryColor)
    doc.text('Resumen del Producto:', rightColX, currentY)

    doc.setFont('helvetica', 'normal')
    doc.setTextColor(...lightTextColor)
    doc.text('Tipo de Producto:', rightColX, currentY + 6)
    doc.text('Moneda:', rightColX, currentY + 12)

    doc.setFont('helvetica', 'bold')
    doc.setTextColor(...textColor)
    doc.text('Crédito Hipotecario (Modelo BCP)', rightColX + 35, currentY + 6)
    doc.text(datosInput.moneda === 'USD' ? 'Dólares (USD)' : 'Soles (PEN)', rightColX + 35, currentY + 12)

    currentY += 25

    const cuotaMonto = datosInput.precioVivienda * (datosInput.cuotaInicialPorcentaje / 100)
    const montoFinanciar = datosInput.precioVivienda - cuotaMonto

    autoTable(doc, {
      startY: currentY,
      head: [['CONDICIONES FINANCIERAS DEL CRÉDITO', '']],
      body: [
        ['Precio de Venta', formatCurrency(datosInput.precioVivienda, datosInput.moneda)],
        ['Valor de Tasación', formatCurrency(datosInput.valorTasacion, datosInput.moneda)],
        ['Cuota Inicial', formatCurrency(cuotaMonto, datosInput.moneda) + ` (${datosInput.cuotaInicialPorcentaje}%)`],
        ['Monto a Financiar', formatCurrency(montoFinanciar, datosInput.moneda)],
        ['Plazo Solicitado', `${datosInput.plazoMeses} meses`],
        ['TEA (%)', formatRate(datosInput.tasaInteres / 100)],
        ['Fecha de la Simulación', formatDate(simulacion.fechaSimulacion)],
      ],
      theme: 'plain',
      headStyles: {
        fillColor: primaryColor,
        textColor: [255, 255, 255],
        fontStyle: 'bold',
        fontSize: 11,
        halign: 'left',
        cellPadding: 8,
      },
      bodyStyles: {
        fontSize: 10,
        textColor: textColor,
        cellPadding: 6,
        lineColor: [230, 230, 230],
        lineWidth: { bottom: 0.1 },
      },
      columnStyles: {
        0: { fontStyle: 'bold', cellWidth: 100 },
        1: { halign: 'right' },
      },
      alternateRowStyles: { fillColor: [248, 250, 252] },
    })

    currentY = doc.lastAutoTable.finalY + 15

    doc.setFontSize(11)
    doc.setFont('helvetica', 'bold')
    doc.setTextColor(...primaryColor)
    doc.text('INDICADORES DE COSTO Y RENTABILIDAD', 14, currentY - 4)

    const vanValue = dataCompleta.van || 0
    const vanFormatted = formatCurrency(Math.abs(vanValue), datosInput.moneda)

    autoTable(doc, {
      startY: currentY,
      body: [
        ['TCEA (Tasa de Costo Efectivo Anual)', formatRate(dataCompleta.tcea)],
        ['TIR Mensual (Tasa Interna de Retorno)', formatRate(dataCompleta.tir)],
        ['Costo Financiero Actualizado (VAN)', vanFormatted],
      ],
      theme: 'plain',
      bodyStyles: {
        fontSize: 10,
        textColor: textColor,
        cellPadding: 6,
        lineColor: [200, 200, 200],
        lineWidth: { bottom: 0.1 },
      },
      columnStyles: {
        0: { fontStyle: 'bold', cellWidth: 100 },
      },
      willDrawCell: function (data) {
        if (data.column.index === 1) {
          data.cell.styles.halign = 'right'
          data.cell.styles.fontStyle = 'bold'
          data.cell.styles.fontSize = 11
          data.cell.styles.textColor = primaryColor
        }
      },
      alternateRowStyles: { fillColor: [248, 250, 252] },
    })

    doc.addPage()
    doc.setFontSize(14)
    doc.setFont('helvetica', 'bold')
    doc.setTextColor(...primaryColor)
    doc.text('CRONOGRAMA DETALLADO DE PAGOS', 14, 20)

    autoTable(doc, {
      startY: 28,
      head: [
        [
          'N°',
          'Tasa Período',
          'Saldo Inicial',
          'Amort.',
          'Interés',
          'Seg. Desg.',
          'Seg. Riesgo',
          'Gastos',
          'Cuota Total',
          'Saldo Final',
        ],
      ],
      body: dataCompleta.cronograma.map((f) => [
        f.nroCuota,
        formatRate(f.tasaPeriodo),
        formatCurrency(f.saldoInicial, datosInput.moneda),
        formatCurrency(f.amortizacion, datosInput.moneda),
        formatCurrency(f.interes, datosInput.moneda),
        formatCurrency(f.segDesgravamen || 0, datosInput.moneda),
        formatCurrency(f.segInmueble || f.seguroRiesgo || 0, datosInput.moneda),
        formatCurrency((f.portes || 0) + (f.gastosAdministracion || 0), datosInput.moneda),
        formatCurrency(f.cuotaTotal, datosInput.moneda),
        formatCurrency(f.saldoFinal, datosInput.moneda),
      ]),
      theme: 'grid',
      styles: { fontSize: 7, textColor: [60, 60, 60] },
      headStyles: { fillColor: primaryColor, textColor: 255, fontStyle: 'bold', halign: 'center' },
      bodyStyles: { halign: 'right' },
      columnStyles: { 0: { halign: 'center', fontStyle: 'bold' } },
      alternateRowStyles: { fillColor: [248, 250, 252] },
      margin: { bottom: 45 },
    })

    const pageCount = doc.internal.getNumberOfPages()
    const pageWidth = doc.internal.pageSize.width

    const disclaimer =
      'IMPORTANTE: Esta simulación es de carácter informativo y preliminar. No constituye una oferta vinculante ni una aprobación de crédito por parte de Banco Hipotecario Demo. Las condiciones finales están estrictamente sujetas a la evaluación crediticia. El Costo Financiero Actualizado (VAN) y la TCEA son referenciales.'
    const maxWidth = pageWidth - 28
    const splitDisclaimer = doc.splitTextToSize(disclaimer, maxWidth)

    for (let i = 1; i <= pageCount; i++) {
      doc.setPage(i)
      const pageHeight = doc.internal.pageSize.height

      if (i === pageCount) {
        doc.setFontSize(8)
        doc.setTextColor(...lightTextColor)
        doc.setFont('helvetica', 'normal')

        const lineHeight = 3.5
        const startY = pageHeight - 15 - splitDisclaimer.length * lineHeight
        doc.text(splitDisclaimer, 14, startY, { lineHeightFactor: 1.2 })
      }

      doc.setDrawColor(...primaryColor)
      doc.setLineWidth(0.5)
      doc.line(14, pageHeight - 12, 196, pageHeight - 12)

      doc.setFontSize(8)
      doc.setTextColor(...primaryColor)
      doc.text('Banco Hipotecario Demo - Documento Oficial de Auditoría', 14, pageHeight - 7)
      doc.text(`Página ${i} de ${pageCount}`, 196, pageHeight - 7, { align: 'right' })
    }

    doc.save(
      `Simulacion_Hipotecaria_BANCO_DEMO_${(simulacion.simulacionId || '').toString().padStart(6, '0')}.pdf`,
    )
  } catch (error) {
    console.error('ERROR CRÍTICO al generar el PDF profesional:', error)
    alert('Fallo en la generación del documento. Por favor, contacte al administrador del sistema.')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  cargarHistorial()
})
</script>

<template>
  <div class="historial-layout">
    <div class="header-section">
      <h2 class="page-title">Auditoría e Historial de Operaciones</h2>
      <p class="page-subtitle">
        Consulte sus registros históricos. Haga clic en una fila para visualizar el detalle o
        descargue el informe oficial.
      </p>
    </div>

    <div v-if="loading" class="state-box loading">
      <span class="spinner"></span> Procesando información del servidor...
    </div>

    <div v-else-if="errorMsg" class="state-box error">
      {{ errorMsg }}
    </div>

    <div v-else-if="historial.length === 0" class="state-box empty">
      No se han encontrado operaciones registradas en su cuenta.
    </div>

    <div v-else class="table-container">
      <table class="financial-table interactive">
        <thead>
          <tr>
            <th>Propiedad</th>
            <th>Divisa</th>
            <th>Fecha de Emisión</th>
            <th>Valor Inmueble</th>
            <th>Monto Préstamo</th>
            <th>TCEA (Ref.)</th>
            <th>Documentación</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="s in historial"
            :key="s.simulacionId"
            @click="recrearSimulacion(s)"
            title="Haga clic para recalcular y ver el cronograma completo"
          >
            <td class="col-id" style="max-width: 150px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;" :title="s.direccionPropiedad || 'Sin Nombre'">{{ s.direccionPropiedad || "Sin Nombre" }}</td>
            <td class="col-divisa">
              <span :class="['badge', s.moneda === 'USD' ? 'badge-usd' : 'badge-pen']">
                {{ s.moneda === 'USD' ? 'USD ($)' : 'PEN (S/)' }}
              </span>
            </td>
            <td>{{ formatDate(s.fechaSimulacion) }}</td>
            <td>{{ formatCurrency(s.precioVenta, s.moneda) }}</td>
            <td class="col-monto">{{ formatCurrency(s.montoPrestamo, s.moneda) }}</td>
            <td class="col-tasa">{{ Number((s.tcea ?? s.Tcea) * 100).toFixed(2) }}%</td>
            <td class="col-actions">
              <button
                @click.stop="editarSimulacion(s)"
                class="btn-editar"
                title="Editar Simulación"
              >
                ✏️ Editar
              </button>
              <button
                @click.stop="descargarPDF(s)"
                class="btn-pdf"
                title="Descargar Informe Oficial en PDF"
              >
                📄 PDF
              </button>
              <button
                @click.stop="confirmarEliminacion(s.simulacionId)"
                title="Eliminar Simulación"
                style="background-color: transparent; border: none; color: #ef4444; font-size: 1.5rem; font-weight: bold; padding: 0 0.5rem; cursor: pointer; line-height: 1;"
              >
                &times;
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.historial-layout {
  padding: 1.5rem;
  background-color: var(--panel-bg, #ffffff);
  border-radius: 12px;
  box-shadow:
    0 10px 15px -3px rgba(0, 0, 0, 0.1),
    0 4px 6px -2px rgba(0, 0, 0, 0.05);
  border: 1px solid var(--border-color, #e2e8f0);
  transition:
    background-color 0.3s,
    border-color 0.3s;
}

.header-section {
  margin-bottom: 2rem;
  border-bottom: 2px solid var(--border-color, #f1f5f9);
  padding-bottom: 1rem;
}

.page-title {
  color: var(--text-title, #0f172a);
  margin: 0 0 0.5rem 0;
  font-size: 1.6rem;
  font-weight: 700;
}

.page-subtitle {
  color: var(--text-muted, #64748b);
  margin: 0;
  font-size: 1rem;
}

.state-box {
  padding: 4rem 2rem;
  text-align: center;
  border-radius: 12px;
  font-weight: 500;
  font-size: 1.1rem;
}

.loading {
  color: #0369a1;
  background-color: #f0f9ff;
  border: 1px solid #bae6fd;
}
.error {
  color: #b91c1c;
  background-color: #fef2f2;
  border: 1px solid #fecaca;
}
.empty {
  color: var(--text-muted, #64748b);
  background-color: var(--kpi-bg, #f8fafc);
  border: 2px dashed var(--fieldset-border, #cbd5e1);
}

.spinner {
  display: inline-block;
  width: 1.2rem;
  height: 1.2rem;
  border: 3px solid rgba(3, 105, 161, 0.3);
  border-radius: 50%;
  border-top-color: #0369a1;
  animation: spin 1s ease-in-out infinite;
  vertical-align: middle;
  margin-right: 0.75rem;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.table-container {
  overflow-x: auto;
  border-radius: 8px;
  border: 1px solid var(--border-color, #e2e8f0);
}

.financial-table {
  width: 100%;
  border-collapse: separate;
  border-spacing: 0;
  text-align: left;
}

.financial-table th {
  background-color: var(--table-head, #0f172a);
  color: var(--table-head-text, #ffffff);
  padding: 1rem 1.25rem;
  font-weight: 600;
  font-size: 0.95rem;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  white-space: nowrap;
}

.financial-table td {
  padding: 1rem 1.25rem;
  border-bottom: 1px solid var(--border-color, #f1f5f9);
  color: var(--text-color, #334155);
  vertical-align: middle;
  font-size: 0.95rem;
}

.interactive tbody tr {
  cursor: pointer;
  transition: all 0.2s ease-in-out;
}

.interactive tbody tr:hover {
  background-color: var(--table-hover, #f1f5f9);
  transform: translateY(-1px);
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
}

.col-id {
  font-weight: 700;
  color: var(--text-title, #0f172a);
  font-family: monospace;
}

.col-monto {
  font-weight: 700;
  color: var(--text-title, #1e293b);
}

.col-divisa {
  text-align: center;
}

.badge {
  display: inline-block;
  padding: 0.25rem 0.6rem;
  border-radius: 9999px;
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.badge-pen {
  background-color: #dcfce7;
  color: #166534;
  border: 1px solid #bbf7d0;
}

.badge-usd {
  background-color: #dbeafe;
  color: #1e40af;
  border: 1px solid #bfdbfe;
}

.col-tasa {
  font-family: monospace;
  color: var(--primary, #0369a1);
  font-weight: 600;
}

.col-actions {
  text-align: center;
  white-space: nowrap;
}

.btn-editar {
  background-color: var(--panel-bg, #ffffff);
  color: var(--primary, #0369a1);
  border: 2px solid var(--primary, #0369a1);
  padding: 0.5rem 1rem;
  border-radius: 6px;
  cursor: pointer;
  font-weight: 700;
  font-size: 0.85rem;
  transition: all 0.2s;
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  margin-right: 0.5rem;
}

.btn-editar:hover {
  background-color: var(--primary, #0369a1);
  color: var(--panel-bg, #ffffff);
}

.btn-pdf {
  background-color: var(--panel-bg, #ffffff);
  color: var(--text-title, #0f172a);
  border: 2px solid var(--text-title, #0f172a);
  padding: 0.5rem 1rem;
  border-radius: 6px;
  cursor: pointer;
  font-weight: 700;
  font-size: 0.85rem;
  transition: all 0.2s;
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
}

.btn-pdf:hover {
  background-color: var(--text-title, #0f172a);
  color: var(--panel-bg, #ffffff);
}
</style>
