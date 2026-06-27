import { useState } from 'react';

/**
 * Hook para gerenciar diálogo de confirmação
 * @returns {Object} Objeto com estado e funções do diálogo
 */
export function useConfirmDialog() {
  const [open, setOpen] = useState(false);
  const [config, setConfig] = useState({
    title: '',
    description: '',
    confirmText: 'Confirmar',
    cancelText: 'Cancelar',
    variant: 'default',
    onConfirm: null,
  });
  const [loading, setLoading] = useState(false);

  const show = (newConfig) => {
    setConfig({
      title: newConfig.title || '',
      description: newConfig.description || '',
      confirmText: newConfig.confirmText || 'Confirmar',
      cancelText: newConfig.cancelText || 'Cancelar',
      variant: newConfig.variant || 'default',
      onConfirm: newConfig.onConfirm || null,
    });
    setOpen(true);
  };

  const hide = () => {
    setOpen(false);
    setLoading(false);
  };

  const handleConfirm = async () => {
    if (config.onConfirm) {
      setLoading(true);
      try {
        await config.onConfirm();
        hide();
      } catch (error) {
        console.error('Erro na confirmação:', error);
        setLoading(false);
      }
    } else {
      hide();
    }
  };

  return {
    open,
    loading,
    config,
    show,
    hide,
    handleConfirm,
  };
}
