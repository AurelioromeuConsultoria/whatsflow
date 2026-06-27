import { useCallback, useEffect, useRef } from 'react';

export function useDebouncedCallback(callback, delayMs = 300) {
  const cbRef = useRef(callback);
  const timeoutRef = useRef(null);

  useEffect(() => {
    cbRef.current = callback;
  }, [callback]);

  useEffect(() => {
    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, []);

  return useCallback(
    (...args) => {
      if (timeoutRef.current) clearTimeout(timeoutRef.current);
      timeoutRef.current = setTimeout(() => cbRef.current(...args), delayMs);
    },
    [delayMs]
  );
}

