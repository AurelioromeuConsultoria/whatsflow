import { renderHook, act } from '@testing-library/react';
import { useTableSort } from '@/hooks/useTableSort';

const data = [{ nome: 'Carlos' }, { nome: 'ana' }, { nome: 'Bruno' }];

describe('useTableSort', () => {
  it('sem campo de ordenação mantém a ordem original', () => {
    const { result } = renderHook(() => useTableSort(data));
    expect(result.current.sortedData).toEqual(data);
  });

  it('ordena ascendente case-insensitive', () => {
    const { result } = renderHook(() => useTableSort(data, { defaultSort: 'nome' }));
    expect(result.current.sortedData.map((d) => d.nome)).toEqual(['ana', 'Bruno', 'Carlos']);
  });

  it('handleSort no mesmo campo alterna para desc', () => {
    const { result } = renderHook(() => useTableSort(data, { defaultSort: 'nome' }));
    act(() => result.current.handleSort('nome'));
    expect(result.current.sortConfig.direction).toBe('desc');
    expect(result.current.sortedData.map((d) => d.nome)).toEqual(['Carlos', 'Bruno', 'ana']);
  });

  it('handleSort em novo campo começa ascendente', () => {
    const { result } = renderHook(() => useTableSort(data, { defaultSort: 'nome', defaultDirection: 'desc' }));
    act(() => result.current.handleSort('idade'));
    expect(result.current.sortConfig).toEqual({ field: 'idade', direction: 'asc' });
  });

  it('não quebra com valores nulos', () => {
    const comNulos = [{ nome: 'Zé' }, { nome: null }, { nome: 'Ana' }];
    const { result } = renderHook(() => useTableSort(comNulos, { defaultSort: 'nome' }));
    expect(result.current.sortedData).toHaveLength(3);
  });
});
