/**
 * Utilitários para exportação de dados
 */

/**
 * Exporta dados para CSV
 * @param {Array} data - Array de objetos para exportar
 * @param {string} filename - Nome do arquivo (sem extensão)
 * @param {Array} columns - Array de objetos { key, label } definindo as colunas
 */
export function exportToCSV(data, filename = 'export', columns = []) {
  if (!data || data.length === 0) {
    console.warn('Nenhum dado para exportar');
    return;
  }

  // Se não foram especificadas colunas, usar todas as chaves do primeiro objeto
  if (columns.length === 0) {
    const firstItem = data[0];
    columns = Object.keys(firstItem).map(key => ({
      key,
      label: key.charAt(0).toUpperCase() + key.slice(1).replace(/([A-Z])/g, ' $1'),
    }));
  }

  // Criar cabeçalho
  const headers = columns.map(col => col.label).join(',');

  // Criar linhas
  const rows = data.map(item => {
    return columns.map(col => {
      let value = item[col.key];
      
      // Tratar valores nulos/undefined
      if (value == null) value = '';
      
      // Tratar objetos/arrays
      if (typeof value === 'object' && value !== null) {
        if (Array.isArray(value)) {
          value = value.map(v => String(v)).join('; ');
        } else {
          value = JSON.stringify(value);
        }
      }
      
      // Escapar vírgulas e aspas
      value = String(value).replace(/"/g, '""');
      if (value.includes(',') || value.includes('"') || value.includes('\n')) {
        value = `"${value}"`;
      }
      
      return value;
    }).join(',');
  });

  // Combinar tudo
  const csvContent = [headers, ...rows].join('\n');

  // Criar blob e download
  const blob = new Blob(['\ufeff' + csvContent], { type: 'text/csv;charset=utf-8;' });
  const link = document.createElement('a');
  const url = URL.createObjectURL(blob);
  
  link.setAttribute('href', url);
  link.setAttribute('download', `${filename}_${new Date().toISOString().split('T')[0]}.csv`);
  link.style.visibility = 'hidden';
  
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  
  URL.revokeObjectURL(url);
}

/**
 * Exporta dados para Excel (formato CSV com extensão .xlsx ou usando biblioteca)
 * Por enquanto, usa CSV que abre no Excel
 * @param {Array} data - Array de objetos para exportar
 * @param {string} filename - Nome do arquivo (sem extensão)
 * @param {Array} columns - Array de objetos { key, label } definindo as colunas
 */
export function exportToExcel(data, filename = 'export', columns = []) {
  // Por enquanto, usa CSV que abre perfeitamente no Excel
  // Se precisar de formatação avançada, pode usar biblioteca como xlsx
  exportToCSV(data, filename, columns);
}

/**
 * Prepara dados para exportação, aplicando filtros e formatação
 * @param {Array} data - Dados originais
 * @param {Function} formatter - Função para formatar cada item: (item) => Object
 */
export function prepareExportData(data, formatter = null) {
  if (!data || data.length === 0) return [];
  
  if (formatter) {
    return data.map(formatter);
  }
  
  return data;
}
