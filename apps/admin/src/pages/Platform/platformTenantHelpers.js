export function hasTenantOperationalContract(tenant) {
  return Boolean(tenant?.statusOperacional) && Number(tenant?.onboardingTotal || 0) > 0;
}

export function buildTenantOnboardingChecklist(tenant) {
  const hasBackendChecklist = hasTenantOperationalContract(tenant);

  const items = [
    {
      id: 'identidade',
      label: 'Identidade básica validada',
      done: hasBackendChecklist ? Boolean(tenant?.onboardingIdentidadeOk) : Boolean(tenant?.nome && tenant?.slug),
    },
    {
      id: 'branding',
      label: 'Branding mínimo configurado',
      done: hasBackendChecklist
        ? Boolean(tenant?.onboardingBrandingOk)
        : Boolean(tenant?.corPrimaria && tenant?.corSecundaria),
    },
    {
      id: 'dominio',
      label: 'Domínio principal configurado',
      done: hasBackendChecklist ? Boolean(tenant?.onboardingDominioOk) : Boolean(tenant?.dominioPrimario),
    },
    {
      id: 'admin',
      label: 'Admin local provisionado',
      done: hasBackendChecklist ? Boolean(tenant?.onboardingAdminOk) : Number(tenant?.totalAdministradores || 0) > 0,
    },
    {
      id: 'operacao',
      label: 'Base inicial operacional criada',
      done: hasBackendChecklist
        ? Boolean(tenant?.onboardingBaseOperacionalOk)
        : Number(tenant?.totalUsuarios || 0) > 0 && Number(tenant?.totalPessoas || 0) > 0,
    },
  ];

  const completed = hasBackendChecklist ? Number(tenant?.onboardingConcluidos || 0) : items.filter((item) => item.done).length;
  const total = hasBackendChecklist ? Number(tenant?.onboardingTotal || items.length) : items.length;
  const progress = hasBackendChecklist
    ? Number(tenant?.onboardingPercentual || 0)
    : total
      ? Math.round((completed / total) * 100)
      : 0;

  return {
    items,
    completed,
    total,
    progress,
  };
}

export function deriveTenantOperationalStatus(tenant) {
  const checklist = buildTenantOnboardingChecklist(tenant);

  if (tenant?.statusOperacional) {
    return {
      key: tenant?.statusOperacionalChave || 'rascunho',
      label: tenant.statusOperacional,
      tone: tenant?.statusOperacionalTom || 'secondary',
      progress: checklist.progress,
    };
  }

  if (!tenant?.ativo) {
    return {
      key: 'inativo',
      label: 'Inativo',
      tone: 'secondary',
      progress: checklist.progress,
    };
  }

  if (checklist.completed === checklist.total) {
    return {
      key: 'homologacao-inicial',
      label: 'Homologação inicial',
      tone: 'default',
      progress: checklist.progress,
    };
  }

  if (checklist.completed >= 3) {
    return {
      key: 'onboarding',
      label: 'Onboarding',
      tone: 'outline',
      progress: checklist.progress,
    };
  }

  if (checklist.completed >= 1) {
    return {
      key: 'provisionado',
      label: 'Provisionado',
      tone: 'secondary',
      progress: checklist.progress,
    };
  }

  return {
    key: 'rascunho',
    label: 'Rascunho',
    tone: 'secondary',
    progress: checklist.progress,
  };
}
