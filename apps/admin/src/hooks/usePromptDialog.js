import { useCallback, useRef, useState } from 'react';

/**
 * Hook para coletar um texto do usuário através de um diálogo (substitui window.prompt).
 * Uso:
 *   const promptDialog = usePromptDialog();
 *   const motivo = await promptDialog.prompt({ title, description, required });
 *   if (motivo === null) return; // cancelado
 * @returns {Object} Estado e funções do diálogo de entrada de texto
 */
export function usePromptDialog() {
  const [open, setOpen] = useState(false);
  const [value, setValue] = useState('');
  const [config, setConfig] = useState({
    title: '',
    description: '',
    label: '',
    placeholder: '',
    confirmText: 'Confirmar',
    cancelText: 'Cancelar',
    required: false,
    multiline: true,
  });
  const resolverRef = useRef(null);

  const prompt = useCallback((newConfig = {}) => {
    setConfig({
      title: newConfig.title || '',
      description: newConfig.description || '',
      label: newConfig.label || '',
      placeholder: newConfig.placeholder || '',
      confirmText: newConfig.confirmText || 'Confirmar',
      cancelText: newConfig.cancelText || 'Cancelar',
      required: newConfig.required || false,
      multiline: newConfig.multiline !== false,
    });
    setValue(newConfig.defaultValue || '');
    setOpen(true);
    return new Promise((resolve) => {
      resolverRef.current = resolve;
    });
  }, []);

  const settle = useCallback((result) => {
    setOpen(false);
    const resolve = resolverRef.current;
    resolverRef.current = null;
    resolve?.(result);
  }, []);

  const handleConfirm = useCallback(() => {
    settle(value);
  }, [settle, value]);

  const handleCancel = useCallback(() => {
    settle(null);
  }, [settle]);

  // Chamado pelo onOpenChange do AlertDialog (Escape, etc.). Fechar = cancelar.
  const onOpenChange = useCallback((nextOpen) => {
    if (!nextOpen) handleCancel();
  }, [handleCancel]);

  return {
    open,
    value,
    setValue,
    config,
    prompt,
    handleConfirm,
    handleCancel,
    onOpenChange,
  };
}
