export const RESOURCES = {
  DASHBOARD: 'dashboard',
  CONTATOS: 'contatos',
  COMUNICACAO: 'comunicacao',
  USUARIOS: 'usuarios',
  PERFIS_ACESSO: 'perfis-acesso',
  CONFIG_MENSAGENS: 'configuracoes-mensagens',
  MENSAGENS_AGENDADAS: 'mensagens-agendadas',
  // Auditoria é controlada no backend sob o recurso de usuários.
  AUDITORIA: 'usuarios',
};

export const ACTIONS = {
  VIEW: 'view',
  EDIT: 'edit',
  DELETE: 'delete',
};

export const ALL_RESOURCES = Object.values(RESOURCES);
