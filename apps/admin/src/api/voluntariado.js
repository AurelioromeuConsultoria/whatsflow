import { api } from '@/lib/apiClient';

export const equipesApi = {
  getAll: () => api.get('/equipes'),
  getById: (id) => api.get(`/equipes/${id}`),
  create: (data) => api.post('/equipes', data),
  update: (id, data) => api.put(`/equipes/${id}`, data),
  delete: (id) => api.delete(`/equipes/${id}`),
};

export const cargosApi = {
  getAll: () => api.get('/cargos'),
  getById: (id) => api.get(`/cargos/${id}`),
  create: (data) => api.post('/cargos', data),
  update: (id, data) => api.put(`/cargos/${id}`, data),
  delete: (id) => api.delete(`/cargos/${id}`),
};

export const voluntariosApi = {
  getAll: () => api.get('/voluntarios'),
  getById: (id) => api.get(`/voluntarios/${id}`),
  getByPessoa: (pessoaId) => api.get(`/voluntarios/pessoa/${pessoaId}`),
  getByEquipe: (equipeId) => api.get(`/voluntarios/equipe/${equipeId}`),
  create: (data) => api.post('/voluntarios', data),
  update: (id, data) => api.put(`/voluntarios/${id}`, data),
  delete: (id) => api.delete(`/voluntarios/${id}`),
};

export const escalasApi = {
  getById: (id) => api.get(`/Escalas/${id}`),
  getMinhas: (params) => api.get('/Escalas/minhas', { params }),
  getHistoricoVoluntarios: (params) => api.get('/Escalas/historico-voluntarios', { params }),
  getByOcorrencia: (eventoOcorrenciaId) => api.get(`/Escalas/ocorrencia/${eventoOcorrenciaId}`),
  getAllByOcorrencia: (eventoOcorrenciaId) => api.get(`/Escalas/ocorrencia/${eventoOcorrenciaId}/escalas`),
  getByOcorrenciaAndEquipe: (eventoOcorrenciaId, equipeId) =>
    api.get(`/Escalas/ocorrencia/${eventoOcorrenciaId}/equipe/${equipeId}`),
  getSugestoes: (escalaId, equipeId) => api.get(`/Escalas/${escalaId}/sugestoes`, { params: { equipeId } }),
  getPlanejamentoMensal: (params) => api.get('/Escalas/planejamento-mensal', { params }),
  gerarPlanejamentoMensalAutomatico: (data) => api.post('/Escalas/planejamento-mensal/gerar-automatico', data),
  criarAlocacaoPlanejamentoMensal: (data) => api.post('/Escalas/planejamento-mensal/alocacoes', data),
  dispararPlanejamentoMensalWhatsApp: (data) => api.post('/Escalas/planejamento-mensal/disparar-whatsapp', data),
  create: (data) => api.post('/Escalas', data),
  update: (id, data) => api.put(`/Escalas/${id}`, data),
  delete: (id) => api.delete(`/Escalas/${id}`),
  addItem: (escalaId, data) => api.post(`/Escalas/${escalaId}/itens`, data),
  updateItem: (escalaId, escalaItemId, data) => api.put(`/Escalas/${escalaId}/itens/${escalaItemId}`, data),
  deleteItem: (escalaId, escalaItemId) => api.delete(`/Escalas/${escalaId}/itens/${escalaItemId}`),
  confirmarItem: (escalaId, escalaItemId) => api.post(`/Escalas/${escalaId}/itens/${escalaItemId}/confirmar`),
  recusarItem: (escalaId, escalaItemId, data) => api.post(`/Escalas/${escalaId}/itens/${escalaItemId}/recusar`, data),
  registrarPresenca: (escalaId, escalaItemId, data) => api.post(`/Escalas/${escalaId}/itens/${escalaItemId}/presenca`, data),
  publicar: (escalaId) => api.post(`/Escalas/${escalaId}/publicar`),
  processarLembretes: () => api.post('/Escalas/lembretes/processar'),
  gerarAutomatico: (eventoOcorrenciaId, equipeId) =>
    api.post(`/Escalas/ocorrencia/${eventoOcorrenciaId}/equipe/${equipeId}/gerar-automatico`),
};

export const solicitacoesTrocasEscalasApi = {
  getAll: (params) => api.get('/SolicitacoesTrocasEscalas', { params }),
  getByEscala: (escalaId) => api.get(`/SolicitacoesTrocasEscalas/escala/${escalaId}`),
  getMinhas: () => api.get('/SolicitacoesTrocasEscalas/minhas'),
  create: (escalaId, escalaItemId, data) => api.post(`/SolicitacoesTrocasEscalas/escala/${escalaId}/item/${escalaItemId}`, data),
  aprovar: (id, data) => api.post(`/SolicitacoesTrocasEscalas/${id}/aprovar`, data),
  rejeitar: (id, data) => api.post(`/SolicitacoesTrocasEscalas/${id}/rejeitar`, data),
};

export const escalasModelosApi = {
  getById: (id) => api.get(`/EscalasModelos/${id}`),
  getByEquipe: (equipeId) => api.get(`/EscalasModelos/equipe/${equipeId}`),
  getByEvento: (eventoId) => api.get(`/EscalasModelos/evento/${eventoId}`),
  getByEventoAndEquipe: (eventoId, equipeId) =>
    api.get('/EscalasModelos/evento-equipe', { params: { eventoId: eventoId ?? undefined, equipeId } }),
  create: (data) => api.post('/EscalasModelos', data),
  update: (id, data) => api.put(`/EscalasModelos/${id}`, data),
  delete: (id) => api.delete(`/EscalasModelos/${id}`),
};

export const indisponibilidadesVoluntariosApi = {
  getById: (id) => api.get(`/IndisponibilidadesVoluntarios/${id}`),
  getByVoluntario: (voluntarioId, params) =>
    api.get(`/IndisponibilidadesVoluntarios/voluntario/${voluntarioId}`, { params }),
  create: (data) => api.post('/IndisponibilidadesVoluntarios', data),
  delete: (id) => api.delete(`/IndisponibilidadesVoluntarios/${id}`),
};
