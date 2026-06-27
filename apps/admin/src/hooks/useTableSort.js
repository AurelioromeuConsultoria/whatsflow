import { useState, useMemo } from 'react';

/**
 * Hook para gerenciar ordenação de tabelas
 * 
 * @param {Array} data - Array de dados para ordenar
 * @param {Object} config - Configuração de ordenação
 * @param {string} config.defaultSort - Campo padrão para ordenação (ex: 'nome')
 * @param {string} config.defaultDirection - Direção padrão ('asc' | 'desc')
 * @returns {Object} - { sortedData, sortConfig, handleSort }
 */
export function useTableSort(data = [], config = {}) {
  const {
    defaultSort = null,
    defaultDirection = 'asc',
  } = config;

  const [sortField, setSortField] = useState(defaultSort);
  const [sortDirection, setSortDirection] = useState(defaultDirection);

  const sortedData = useMemo(() => {
    if (!sortField || !data || data.length === 0) {
      return data;
    }

    return [...data].sort((a, b) => {
      let aValue = a[sortField];
      let bValue = b[sortField];

      // Tratar valores nulos/undefined
      if (aValue == null) aValue = '';
      if (bValue == null) bValue = '';

      // Tratar objetos aninhados (ex: pessoa.nome)
      if (typeof aValue === 'object' && aValue !== null) {
        aValue = String(aValue);
      }
      if (typeof bValue === 'object' && bValue !== null) {
        bValue = String(bValue);
      }

      // Converter para string para comparação
      aValue = String(aValue).toLowerCase();
      bValue = String(bValue).toLowerCase();

      // Comparação
      if (aValue < bValue) {
        return sortDirection === 'asc' ? -1 : 1;
      }
      if (aValue > bValue) {
        return sortDirection === 'asc' ? 1 : -1;
      }
      return 0;
    });
  }, [data, sortField, sortDirection]);

  const handleSort = (field) => {
    if (sortField === field) {
      // Alternar direção se já está ordenando por este campo
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      // Novo campo, começar com ascendente
      setSortField(field);
      setSortDirection('asc');
    }
  };

  return {
    sortedData,
    sortConfig: {
      field: sortField,
      direction: sortDirection,
    },
    handleSort,
  };
}
