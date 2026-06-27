import { useState, useEffect, useMemo } from 'react';

/**
 * Hook para gerenciar paginação client-side
 * @param {Array} items - Array de itens para paginar
 * @param {number} defaultPageSize - Tamanho padrão da página
 * @returns {Object} Objeto com dados e funções de paginação
 */
export function usePagination(items = [], defaultPageSize = 20) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(defaultPageSize);

  // Reset page when items change
  useEffect(() => {
    setPage(1);
  }, [items.length]);

  const paginatedItems = useMemo(() => {
    const startIndex = (page - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    return items.slice(startIndex, endIndex);
  }, [items, page, pageSize]);

  const totalPages = Math.ceil(items.length / pageSize);

  const handlePageSizeChange = (newSize) => {
    setPageSize(newSize);
    setPage(1);
  };

  return {
    page,
    pageSize,
    total: items.length,
    totalPages,
    paginatedItems,
    setPage,
    setPageSize: handlePageSizeChange,
  };
}
