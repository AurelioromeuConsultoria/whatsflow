import { renderHook, act } from '@testing-library/react';
import { usePagination } from '@/hooks/usePagination';

const items = Array.from({ length: 45 }, (_, i) => i + 1);

describe('usePagination', () => {
  it('pagina o primeiro lote e calcula totalPages', () => {
    const { result } = renderHook(() => usePagination(items, 20));
    expect(result.current.total).toBe(45);
    expect(result.current.totalPages).toBe(3);
    expect(result.current.paginatedItems).toHaveLength(20);
    expect(result.current.paginatedItems[0]).toBe(1);
  });

  it('navega para a página seguinte', () => {
    const { result } = renderHook(() => usePagination(items, 20));
    act(() => result.current.setPage(2));
    expect(result.current.paginatedItems[0]).toBe(21);
    expect(result.current.paginatedItems).toHaveLength(20);
  });

  it('última página traz o restante', () => {
    const { result } = renderHook(() => usePagination(items, 20));
    act(() => result.current.setPage(3));
    expect(result.current.paginatedItems).toHaveLength(5);
  });

  it('mudar o pageSize volta para a página 1', () => {
    const { result } = renderHook(() => usePagination(items, 20));
    act(() => result.current.setPage(2));
    act(() => result.current.setPageSize(10));
    expect(result.current.page).toBe(1);
    expect(result.current.pageSize).toBe(10);
    expect(result.current.totalPages).toBe(5);
  });
});
