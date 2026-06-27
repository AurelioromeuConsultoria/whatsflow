export const OCORRENCIA_TIPOS = [
  { value: 'FEBRE' },
  { value: 'QUEDA' },
  { value: 'CHORO' },
  { value: 'TROCA_SALA' },
  { value: 'MEDICACAO' },
  { value: 'COMPORTAMENTO' },
  { value: 'OUTRO' },
];

export function formatOcorrenciaTipo(tipo, t) {
  const item = OCORRENCIA_TIPOS.find((entry) => entry.value === tipo);
  return item ? t(`kids.occurrence.types.${item.value}`) : tipo;
}

export function getOcorrenciaStatusConfig(status, t) {
  const chave = (status || '').toLowerCase();

  const statusConfig = {
    aberta: {
      label: t('kids.occurrence.status.open'),
      className: 'bg-amber-500 hover:bg-amber-600',
    },
    em_andamento: {
      label: t('kids.occurrence.status.inProgress'),
      className: 'bg-blue-500 hover:bg-blue-600',
    },
    encerrada: {
      label: t('kids.occurrence.status.closed'),
      className: 'bg-emerald-600 hover:bg-emerald-700',
    },
  };

  return statusConfig[chave] || {
    label: status || t('kids.occurrence.status.noStatus'),
    className: 'bg-slate-500 hover:bg-slate-600',
  };
}

export function isOcorrenciaEncerrada(status) {
  return (status || '').toLowerCase() === 'encerrada';
}

export function buildCriticalDescription(crianca, t) {
  const itens = [];
  if (crianca.temAlergia) itens.push(t('kids.panel.allergy', 'Alergia'));
  if (crianca.temRestricao) itens.push(t('kids.panel.restriction', 'Restrição'));
  if (crianca.temObservacaoCritica) itens.push(t('kids.panel.criticalNote', 'Observação crítica'));
  return itens.join(' • ');
}
