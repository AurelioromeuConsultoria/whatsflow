import { renderHook, act } from '@testing-library/react';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';

describe('useConfirmDialog', () => {
  it('começa fechado e abre com a config no show', () => {
    const { result } = renderHook(() => useConfirmDialog());
    expect(result.current.open).toBe(false);

    act(() => result.current.show({ title: 'Excluir?', onConfirm: vi.fn() }));

    expect(result.current.open).toBe(true);
    expect(result.current.config.title).toBe('Excluir?');
    expect(result.current.config.confirmText).toBe('Confirmar'); // default
  });

  it('executa onConfirm e fecha no sucesso', async () => {
    const onConfirm = vi.fn().mockResolvedValue(undefined);
    const { result } = renderHook(() => useConfirmDialog());
    act(() => result.current.show({ onConfirm }));

    await act(async () => {
      await result.current.handleConfirm();
    });

    expect(onConfirm).toHaveBeenCalledTimes(1);
    expect(result.current.open).toBe(false);
    expect(result.current.loading).toBe(false);
  });

  it('mantém aberto e desliga o loading quando onConfirm falha', async () => {
    const onConfirm = vi.fn().mockRejectedValue(new Error('falhou'));
    const { result } = renderHook(() => useConfirmDialog());
    act(() => result.current.show({ onConfirm }));

    await act(async () => {
      await result.current.handleConfirm();
    });

    expect(result.current.open).toBe(true);
    expect(result.current.loading).toBe(false);
  });

  it('hide fecha o diálogo', () => {
    const { result } = renderHook(() => useConfirmDialog());
    act(() => result.current.show({ title: 'X', onConfirm: vi.fn() }));
    act(() => result.current.hide());
    expect(result.current.open).toBe(false);
  });
});
