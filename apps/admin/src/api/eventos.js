import { api } from '@/lib/apiClient';

/** Normaliza evento da API (aceita PascalCase ou camelCase e objeto dentro de dto). */
export function normalizeEvento(e) {
  if (!e) return e;
  const d = e?.dto ?? e;
  return {
    ...d,
    id: d.id,
    titulo: d.titulo ?? d.Titulo ?? '',
    descricao: d.descricao ?? d.Descricao ?? '',
    dataInicio: d.dataInicio ?? d.DataInicio,
    dataFim: d.dataFim ?? d.DataFim,
    url: d.url ?? d.Url ?? '',
    imagemDestaque: d.imagemDestaque ?? d.ImagemDestaque ?? '',
    tipo: d.tipo ?? d.Tipo ?? 1,
    tipoDescricao: d.tipoDescricao ?? d.TipoDescricao ?? 'Evento',
    ehRecorrente: d.ehRecorrente ?? d.EhRecorrente ?? false,
    ativo: d.ativo ?? d.Ativo ?? true,
    aceitaInscricoes: d.aceitaInscricoes ?? d.AceitaInscricoes ?? false,
    configuracaoFormularioInscricao: d.configuracaoFormularioInscricao ?? d.ConfiguracaoFormularioInscricao ?? null,
  };
}

export const eventosApi = {
  getAll: () => api.get('/Eventos'),
  getById: (id) => api.get(`/Eventos/${id}`),
  create: (data) => api.post('/Eventos', data),
  update: (id, data) => api.put(`/Eventos/${id}`, data),
  delete: (id) => api.delete(`/Eventos/${id}`),
  getByPeriodo: () => api.get('/Eventos/periodo'),
};

/** Recorrências do evento (dia da semana, horário, periodicidade). */
export const eventosRecorrenciasApi = {
  getByEvento: (eventoId) => api.get(`/Eventos/${eventoId}/recorrencias`),
  getById: (eventoId, id) => api.get(`/Eventos/${eventoId}/recorrencias/${id}`),
  create: (eventoId, data) => api.post(`/Eventos/${eventoId}/recorrencias`, data),
  update: (eventoId, id, data) => api.put(`/Eventos/${eventoId}/recorrencias/${id}`, data),
  delete: (eventoId, id) => api.delete(`/Eventos/${eventoId}/recorrencias/${id}`),
};

export const eventosOcorrenciasApi = {
  getByEvento: (eventoId) => api.get(`/EventosOcorrencias/evento/${eventoId}`),
  getByPeriodo: (dataInicio, dataFim, eventoId) =>
    api.get('/EventosOcorrencias/periodo', { params: { dataInicio, dataFim, eventoId } }),
  getCoberturaVoluntariado: (params) =>
    api.get('/EventosOcorrencias/periodo/cobertura-voluntariado', { params }),
  getById: (id) => api.get(`/EventosOcorrencias/${id}`),
  create: (data) => api.post('/EventosOcorrencias', data),
  update: (id, data) => api.put(`/EventosOcorrencias/${id}`),
  delete: (id) => api.delete(`/EventosOcorrencias/${id}`),
  gerarRecorrencia: (eventoId, dataInicio, dataFim) =>
    api.post('/EventosOcorrencias/gerar-recorrencia', null, { params: { eventoId, dataInicio, dataFim } }),
};

export const inscricoesEventosApi = {
  getAll: () => api.get('/InscricoesEventos'),
  getById: (id) => api.get(`/InscricoesEventos/${id}`),
  getByEvento: (eventoId) => api.get(`/InscricoesEventos/evento/${eventoId}`),
  getByStatus: (status) => api.get(`/InscricoesEventos/status/${status}`),
  getEstatisticas: (eventoId) => api.get(`/InscricoesEventos/evento/${eventoId}/estatisticas`),
  create: (data) => api.post('/InscricoesEventos', data),
  update: (id, data) => api.put(`/InscricoesEventos/${id}`),
  confirmar: (id) => api.put(`/InscricoesEventos/${id}/confirmar`),
  cancelar: (id) => api.put(`/InscricoesEventos/${id}/cancelar`),
  delete: (id) => api.delete(`/InscricoesEventos/${id}`),
};
