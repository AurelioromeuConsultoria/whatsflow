import { renderHook } from '@testing-library/react';
import { useDebouncedCallback } from '@/hooks/useDebouncedCallback';

describe('useDebouncedCallback', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('dispara uma única vez após o delay, com os últimos argumentos', () => {
    const fn = vi.fn();
    const { result } = renderHook(() => useDebouncedCallback(fn, 300));

    result.current('a');
    result.current('b');
    expect(fn).not.toHaveBeenCalled();

    vi.advanceTimersByTime(300);

    expect(fn).toHaveBeenCalledTimes(1);
    expect(fn).toHaveBeenCalledWith('b');
  });

  it('reinicia a contagem a cada chamada', () => {
    const fn = vi.fn();
    const { result } = renderHook(() => useDebouncedCallback(fn, 300));

    result.current('x');
    vi.advanceTimersByTime(200);
    result.current('y');
    vi.advanceTimersByTime(200); // total 400, mas só 200 desde a última
    expect(fn).not.toHaveBeenCalled();

    vi.advanceTimersByTime(100);
    expect(fn).toHaveBeenCalledTimes(1);
    expect(fn).toHaveBeenCalledWith('y');
  });
});
