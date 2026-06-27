import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Building2, Palette, Pencil, Plus, RefreshCw, Search, Shield, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Switch } from '@/components/ui/switch';
import { Progress } from '@/components/ui/progress';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { PageEmptyState } from '@/components/ui/page-state';
import { useAuth } from '@/context/AuthContext';
import { tenantsApi, uploadApi } from '@/lib/api';
import { getApiErrorMessage } from '@/lib/apiError';
import { formatDateTime } from '@/lib/formatters';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { buildTenantOnboardingChecklist, deriveTenantOperationalStatus, hasTenantOperationalContract } from './platformTenantHelpers';

const initialForm = {
  nome: '',
  nomeExibicao: '',
  slug: '',
  dominioPrimario: '',
  logoUrl: '',
  faviconUrl: '',
  corPrimaria: '#111827',
  corSecundaria: '#374151',
  adminNome: '',
  adminEmail: '',
  adminTelefone: '',
  adminWhatsApp: '',
  adminEmailLogin: '',
  adminSenha: '',
};

function StatusBadge({ ativo, t }) {
  return <Badge variant={ativo ? 'default' : 'secondary'}>{t(ativo ? 'platformTenants.status.active' : 'platformTenants.status.inactive')}</Badge>;
}

export default function TenantsPage() {
  const { t } = useTranslation();
  const { isPlatformAdmin, currentTenant, homeTenant } = useAuth();
  const [tenants, setTenants] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [showProvisionDialog, setShowProvisionDialog] = useState(false);
  const [showEditDialog, setShowEditDialog] = useState(false);
  const [form, setForm] = useState(initialForm);
  const [editingTenant, setEditingTenant] = useState(null);
  const [uploadingLogo, setUploadingLogo] = useState(false);
  const [provisionStep, setProvisionStep] = useState(0);
  const [provisionErrors, setProvisionErrors] = useState({});
  const [provisionResult, setProvisionResult] = useState(null);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('todos');
  const confirmDialog = useConfirmDialog();

  const provisionSteps = useMemo(
    () => [
      { id: 'identidade', title: t('platformTenants.provision.steps.identity.title'), description: t('platformTenants.provision.steps.identity.description') },
      { id: 'branding', title: t('platformTenants.provision.steps.branding.title'), description: t('platformTenants.provision.steps.branding.description') },
      { id: 'admin', title: t('platformTenants.provision.steps.admin.title'), description: t('platformTenants.provision.steps.admin.description') },
      { id: 'revisao', title: t('platformTenants.provision.steps.review.title'), description: t('platformTenants.provision.steps.review.description') },
    ],
    [t],
  );

  const statusFilterOptions = useMemo(
    () => [
      ['todos', t('platformTenants.filters.all')],
      ['ativos', t('platformTenants.filters.active')],
      ['inativos', t('platformTenants.filters.inactive')],
      ['raiz', t('platformTenants.filters.root')],
    ],
    [t],
  );

  const getOperationalStatusLabel = (statusKey) => {
    const keyMap = {
      inativo: 'inactive',
      'homologacao-inicial': 'initialValidation',
      onboarding: 'onboarding',
      provisionado: 'provisioned',
      rascunho: 'draft',
    };

    return t(`platformTenants.operationalStatus.${keyMap[statusKey] || 'draft'}`);
  };

  const stats = useMemo(() => {
    const ativos = tenants.filter((tenant) => tenant.ativo).length;
    return {
      total: tenants.length,
      ativos,
      inativos: tenants.length - ativos,
      adminsLocais: tenants.reduce((acc, tenant) => acc + (tenant.totalAdministradores || 0), 0),
    };
  }, [tenants]);

  const filteredTenants = useMemo(() => {
    const term = search.trim().toLowerCase();

    return tenants.filter((tenant) => {
      const statusMatches =
        statusFilter === 'todos' ||
        (statusFilter === 'ativos' && tenant.ativo) ||
        (statusFilter === 'inativos' && !tenant.ativo) ||
        (statusFilter === 'raiz' && tenant.isRootTenant);

      if (!statusMatches) return false;

      if (!term) return true;

      return [tenant.nome, tenant.nomeExibicao, tenant.slug, tenant.dominioPrimario]
        .filter(Boolean)
        .join(' ')
        .toLowerCase()
        .includes(term);
    });
  }, [search, statusFilter, tenants]);

  const load = async ({ silent = false } = {}) => {
    try {
      if (silent) setRefreshing(true);
      else setLoading(true);
      setError(null);
      const response = await tenantsApi.getAll();
      setTenants(response.data || []);
    } catch (err) {
      setError(getApiErrorMessage(err, t('platformTenants.errorLoad')));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    if (isPlatformAdmin) {
      load();
    } else {
      setLoading(false);
    }
  }, [isPlatformAdmin]);

  const handleFormChange = (field, value) => {
    setForm((current) => ({ ...current, [field]: value }));
    setProvisionErrors((current) => {
      if (!current[field]) return current;
      const next = { ...current };
      delete next[field];
      return next;
    });
  };

  const resetProvisionDialogState = () => {
    setForm(initialForm);
    setProvisionStep(0);
    setProvisionErrors({});
    setProvisionResult(null);
    setUploadingLogo(false);
  };

  const handleProvisionDialogChange = (open) => {
    setShowProvisionDialog(open);
    if (!open) {
      resetProvisionDialogState();
    }
  };

  const openProvisionDialog = () => {
    resetProvisionDialogState();
    setShowProvisionDialog(true);
  };

  const buildProvisionPayload = () => ({
    ...form,
    nomeExibicao: form.nomeExibicao.trim() || null,
    dominioPrimario: form.dominioPrimario.trim() || null,
    logoUrl: form.logoUrl.trim() || null,
    faviconUrl: form.faviconUrl.trim() || null,
    corPrimaria: form.corPrimaria.trim() || null,
    corSecundaria: form.corSecundaria.trim() || null,
    adminTelefone: form.adminTelefone.trim() || null,
    adminWhatsApp: form.adminWhatsApp.trim() || null,
  });

  const validateProvisionStep = (stepIndex) => {
    const errors = {};

    if (stepIndex === 0) {
      if (!form.nome.trim()) errors.nome = t('platformTenants.validation.nameRequired');
      if (!form.slug.trim()) errors.slug = t('platformTenants.validation.slugRequired');
    }

    if (stepIndex === 1) {
      if (!form.corPrimaria.trim()) errors.corPrimaria = t('platformTenants.validation.primaryColorRequired');
      if (!form.corSecundaria.trim()) errors.corSecundaria = t('platformTenants.validation.secondaryColorRequired');
    }

    if (stepIndex === 2) {
      if (!form.adminNome.trim()) errors.adminNome = t('platformTenants.validation.adminNameRequired');
      if (!form.adminEmail.trim()) errors.adminEmail = t('platformTenants.validation.adminEmailRequired');
      if (!form.adminEmailLogin.trim()) errors.adminEmailLogin = t('platformTenants.validation.loginEmailRequired');
      if (!form.adminSenha.trim()) errors.adminSenha = t('platformTenants.validation.passwordRequired');
    }

    setProvisionErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleProvisionNextStep = () => {
    if (!validateProvisionStep(provisionStep)) return;
    setProvisionStep((current) => Math.min(current + 1, provisionSteps.length - 1));
  };

  const handleProvisionPreviousStep = () => {
    setProvisionStep((current) => Math.max(current - 1, 0));
  };

  const handleProvision = async () => {
    if (!validateProvisionStep(2)) {
      setProvisionStep(2);
      return;
    }

    try {
      setSaving(true);
      const payload = buildProvisionPayload();

      const response = await tenantsApi.create(payload);
      const novoTenant = response.data?.tenant;

      toast.success(t('platformTenants.provision.successToast', { name: novoTenant?.nome || payload.nome }));
      setProvisionResult(response.data || null);
      await load({ silent: true });
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('platformTenants.provision.errorToast')));
    } finally {
      setSaving(false);
    }
  };

  const openEditDialog = (tenant) => {
    setEditingTenant(tenant);
    setForm({
      nome: tenant.nome || '',
      nomeExibicao: tenant.nomeExibicao || '',
      slug: tenant.slug || '',
      dominioPrimario: tenant.dominioPrimario || '',
      logoUrl: tenant.logoUrl || '',
      faviconUrl: tenant.faviconUrl || '',
      corPrimaria: tenant.corPrimaria || '#111827',
      corSecundaria: tenant.corSecundaria || '#374151',
      adminNome: '',
      adminEmail: '',
      adminTelefone: '',
      adminWhatsApp: '',
      adminEmailLogin: '',
      adminSenha: '',
    });
    setShowEditDialog(true);
  };

  const handleUpdate = async () => {
    if (!editingTenant) return;

    try {
      setSaving(true);
      await tenantsApi.update(editingTenant.id, {
        nome: form.nome,
        nomeExibicao: form.nomeExibicao.trim() || null,
        slug: form.slug,
        dominioPrimario: form.dominioPrimario.trim() || null,
        logoUrl: form.logoUrl.trim() || null,
        faviconUrl: form.faviconUrl.trim() || null,
        corPrimaria: form.corPrimaria.trim() || null,
        corSecundaria: form.corSecundaria.trim() || null,
      });

      toast.success(t('platformTenants.edit.successToast', { name: form.nome }));
      setShowEditDialog(false);
      setEditingTenant(null);
      setForm(initialForm);
      await load({ silent: true });
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('platformTenants.edit.errorToast')));
    } finally {
      setSaving(false);
    }
  };

  const handleToggleStatus = async (tenant) => {
    try {
      await tenantsApi.updateStatus(tenant.id, !tenant.ativo);
      toast.success(
        t(tenant.ativo ? 'platformTenants.status.deactivatedToast' : 'platformTenants.status.activatedToast', {
          name: tenant.nome,
        }),
      );
      await load({ silent: true });
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('platformTenants.status.errorToast')));
    }
  };

  const handleDelete = async (tenant) => {
    confirmDialog.show({
      title: t('platformTenants.delete.title'),
      description: t('platformTenants.delete.description', { name: tenant.nome }),
      confirmText: t('platformTenants.delete.confirm'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        await tenantsApi.delete(tenant.id);
        toast.success(t('platformTenants.delete.successToast', { name: tenant.nome }));
        await load({ silent: true });
      },
    });
  };

  const handleLogoUpload = async (event) => {
    const file = event.target.files?.[0];
    if (!file) return;

    try {
      setUploadingLogo(true);
      const formData = new FormData();
      formData.append('file', file);
      const response = await uploadApi.uploadImage(formData);
      const uploadedUrl = response.data?.url || response.data?.path || response.data || '';
      handleFormChange('logoUrl', uploadedUrl);
      toast.success(t('platformTenants.logoUpload.successToast'));
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('platformTenants.logoUpload.errorToast')));
    } finally {
      setUploadingLogo(false);
      event.target.value = '';
    }
  };

  if (!isPlatformAdmin) {
    return <ErrorPage message={t('platformTenants.restricted')} />;
  }

  if (loading) return <LoadingPage text={t('platformTenants.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={() => load()} />;

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold">{t('platformTenants.title')}</h1>
          <p className="mt-1 text-muted-foreground">
            {t('platformTenants.subtitle', {
              homeTenant: homeTenant?.nomeExibicao || homeTenant?.nome || homeTenant?.slug || t('platformTenants.notAvailable'),
              currentTenant: currentTenant?.nomeExibicao || currentTenant?.nome || currentTenant?.slug || t('platformTenants.notAvailable'),
            })}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" onClick={() => load({ silent: true })} disabled={refreshing}>
            <RefreshCw className={`mr-2 h-4 w-4 ${refreshing ? 'animate-spin' : ''}`} />
            {t('actions.refresh')}
          </Button>
          <Button onClick={openProvisionDialog}>
            <Plus className="mr-2 h-4 w-4" />
            {t('platformTenants.new')}
          </Button>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{t('platformTenants.stats.total')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{stats.total}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{t('platformTenants.stats.active')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{stats.ativos}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{t('platformTenants.stats.inactive')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{stats.inativos}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{t('platformTenants.stats.localAdmins')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{stats.adminsLocais}</div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('platformTenants.listTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="mb-4 flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <div className="relative w-full md:max-w-sm">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder={t('platformTenants.searchPlaceholder')}
                className="pl-9"
              />
            </div>
            <div className="flex flex-wrap items-center gap-2">
              {statusFilterOptions.map(([value, label]) => (
                <Button
                  key={value}
                  type="button"
                  variant={statusFilter === value ? 'default' : 'outline'}
                  size="sm"
                  onClick={() => setStatusFilter(value)}
                >
                  {label}
                </Button>
              ))}
            </div>
          </div>

          {!filteredTenants.length ? (
            <PageEmptyState
              title={t('platformTenants.empty.title')}
              description={t('platformTenants.empty.description')}
              action={
                <Button onClick={openProvisionDialog}>
                  <Plus className="mr-2 h-4 w-4" />
                  {t('platformTenants.empty.action')}
                </Button>
              }
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('platformTenants.table.name')}</TableHead>
                  <TableHead>{t('platformTenants.table.displayName')}</TableHead>
                  <TableHead>{t('platformTenants.table.slug')}</TableHead>
                  <TableHead>{t('platformTenants.table.logo')}</TableHead>
                  <TableHead>{t('platformTenants.table.domain')}</TableHead>
                  <TableHead>{t('platformTenants.table.support')}</TableHead>
                  <TableHead>{t('platformTenants.table.status')}</TableHead>
                  <TableHead>{t('platformTenants.table.lastActivity')}</TableHead>
                  <TableHead className="text-right">{t('platformTenants.table.operations')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredTenants.map((tenant) => {
                  const isInitialTenant = !!tenant.isRootTenant;
                  const operationalStatus = deriveTenantOperationalStatus(tenant);
                  const onboarding = buildTenantOnboardingChecklist(tenant);
                  const hasOperationalContract = hasTenantOperationalContract(tenant);
                  return (
                    <TableRow key={tenant.id}>
                      <TableCell className="font-medium">
                        <div className="flex items-center gap-2">
                          <Building2 className="h-4 w-4 text-muted-foreground" />
                          <Link to={`/plataforma/tenants/${tenant.id}`} className="hover:underline">
                            {tenant.nome}
                          </Link>
                        </div>
                      </TableCell>
                      <TableCell>{tenant.nomeExibicao || '-'}</TableCell>
                      <TableCell>{tenant.slug}</TableCell>
                      <TableCell>
                        {tenant.logoUrl ? (
                          <img
                            src={tenant.logoUrl}
                            alt={t('platformTenants.alt.logoWithName', { name: tenant.nome })}
                            className="h-10 w-10 rounded-md border object-contain bg-white p-1"
                          />
                        ) : (
                          '-'
                        )}
                      </TableCell>
                      <TableCell>{tenant.dominioPrimario || '-'}</TableCell>
                      <TableCell>
                        <div className="space-y-1 text-xs text-muted-foreground">
                          <div>{t('platformTenants.support.users', { count: tenant.totalUsuarios || 0 })}</div>
                          <div>{t('platformTenants.support.people', { count: tenant.totalPessoas || 0 })}</div>
                          <div>{t('platformTenants.support.admins', { count: tenant.totalAdministradores || 0 })}</div>
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="space-y-2">
                          <StatusBadge ativo={tenant.ativo} t={t} />
                          <div>
                            <Badge variant={operationalStatus.tone}>{getOperationalStatusLabel(operationalStatus.key)}</Badge>
                          </div>
                          <div className="text-xs text-muted-foreground">
                            {t('platformTenants.operationalStatus.progressSummary', {
                              completed: onboarding.completed,
                              total: onboarding.total,
                              progress: operationalStatus.progress,
                            })}
                          </div>
                          {!hasOperationalContract && (
                            <div className="text-xs text-amber-600">
                              {t('platformTenants.operationalStatus.localRead')}
                            </div>
                          )}
                        </div>
                      </TableCell>
                      <TableCell>{tenant.ultimaAtividadeEm ? formatDateTime(tenant.ultimaAtividadeEm) : '-'}</TableCell>
                      <TableCell className="text-right">
                        <div className="flex items-center justify-end gap-3">
                          <Button variant="outline" size="sm" asChild>
                            <Link to={`/plataforma/tenants/${tenant.id}`}>
                              {t('platformTenants.actions.details')}
                            </Link>
                          </Button>
                          <Button variant="outline" size="sm" onClick={() => openEditDialog(tenant)}>
                            <Pencil className="mr-2 h-3.5 w-3.5" />
                            {t('actions.edit')}
                          </Button>
                          {tenant.canDelete !== false && (
                            <Button variant="outline" size="sm" onClick={() => handleDelete(tenant)}>
                              <Trash2 className="mr-2 h-3.5 w-3.5" />
                              {t('platformTenants.delete.confirm')}
                            </Button>
                          )}
                          <span className="text-xs text-muted-foreground">
                            {isInitialTenant ? t('platformTenants.status.root') : t(tenant.ativo ? 'platformTenants.status.active' : 'platformTenants.status.inactive')}
                          </span>
                          <Switch
                            checked={tenant.ativo}
                            onCheckedChange={() => handleToggleStatus(tenant)}
                            disabled={tenant.canDeactivate === false}
                            aria-label={t('platformTenants.status.toggleAria', { name: tenant.nome })}
                          />
                        </div>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <Dialog open={showProvisionDialog} onOpenChange={handleProvisionDialogChange}>
        <DialogContent className="sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>{t('platformTenants.provision.title')}</DialogTitle>
            <DialogDescription>
              {t('platformTenants.provision.description')}
            </DialogDescription>
          </DialogHeader>

          {provisionResult ? (
            <div className="space-y-5">
              <div className="rounded-xl border border-emerald-500/20 bg-emerald-500/5 p-4">
                <div className="text-sm font-semibold text-emerald-700 dark:text-emerald-400">
                  {t('platformTenants.provision.result.title')}
                </div>
                <p className="mt-1 text-sm text-muted-foreground">
                  {t('platformTenants.provision.result.description')}
                </p>
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <Card>
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm">{t('platformTenants.provision.result.tenantCreated')}</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-1 text-sm">
                    <div><span className="text-muted-foreground">{t('platformTenants.fields.name')}:</span> {provisionResult.tenant?.nome || '-'}</div>
                    <div><span className="text-muted-foreground">{t('platformTenants.fields.slug')}:</span> {provisionResult.tenant?.slug || '-'}</div>
                    <div><span className="text-muted-foreground">{t('platformTenants.fields.id')}:</span> {provisionResult.tenant?.id || '-'}</div>
                  </CardContent>
                </Card>
                <Card>
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm">{t('platformTenants.provision.result.resources')}</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-1 text-sm">
                    <div><span className="text-muted-foreground">{t('platformTenants.fields.profile')}:</span> {provisionResult.perfilAcessoId || '-'}</div>
                    <div><span className="text-muted-foreground">{t('platformTenants.fields.adminPerson')}:</span> {provisionResult.pessoaId || '-'}</div>
                    <div><span className="text-muted-foreground">{t('platformTenants.fields.adminUser')}:</span> {provisionResult.usuarioId || '-'}</div>
                  </CardContent>
                </Card>
              </div>

              <div className="rounded-xl border bg-muted/30 p-4 text-sm text-muted-foreground">
                {t('platformTenants.provision.result.nextStep')}
              </div>
            </div>
          ) : (
            <>
              <div className="space-y-4">
                <div className="space-y-3">
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <div>
                      <div className="text-sm font-medium">{provisionSteps[provisionStep].title}</div>
                      <div className="text-sm text-muted-foreground">{provisionSteps[provisionStep].description}</div>
                    </div>
                    <Badge variant="outline">
                      {t('platformTenants.provision.stepIndicator', {
                        current: provisionStep + 1,
                        total: provisionSteps.length,
                      })}
                    </Badge>
                  </div>
                  <Progress value={((provisionStep + 1) / provisionSteps.length) * 100} />
                  <div className="grid gap-2 md:grid-cols-4">
                    {provisionSteps.map((step, index) => (
                      <button
                        key={step.id}
                        type="button"
                        className={`rounded-lg border px-3 py-2 text-left text-sm transition-colors ${
                          index === provisionStep
                            ? 'border-primary bg-primary/5 text-foreground'
                            : index < provisionStep
                              ? 'border-emerald-500/30 bg-emerald-500/5 text-foreground'
                              : 'border-border text-muted-foreground'
                        }`}
                        onClick={() => {
                          if (index <= provisionStep) setProvisionStep(index);
                        }}
                      >
                        <div className="font-medium">{step.title}</div>
                        <div className="mt-1 text-xs">{step.description}</div>
                      </button>
                    ))}
                  </div>
                </div>

                {provisionStep === 0 && (
                  <div className="grid gap-4 md:grid-cols-2">
                    <div className="space-y-2">
                      <label className="text-sm font-medium">{t('platformTenants.fields.churchName')}</label>
                      <Input value={form.nome} onChange={(e) => handleFormChange('nome', e.target.value)} placeholder={t('platformTenants.placeholders.churchName')} />
                      {provisionErrors.nome && <p className="text-xs text-destructive">{provisionErrors.nome}</p>}
                    </div>
                    <div className="space-y-2">
                      <label className="text-sm font-medium">{t('platformTenants.fields.displayName')}</label>
                      <Input value={form.nomeExibicao} onChange={(e) => handleFormChange('nomeExibicao', e.target.value)} placeholder={t('platformTenants.placeholders.displayNameProvision')} />
                    </div>
                    <div className="space-y-2 md:col-span-2">
                      <label className="text-sm font-medium">{t('platformTenants.fields.slug')}</label>
                      <Input value={form.slug} onChange={(e) => handleFormChange('slug', e.target.value)} placeholder={t('platformTenants.placeholders.slug')} />
                      {provisionErrors.slug && <p className="text-xs text-destructive">{provisionErrors.slug}</p>}
                    </div>
                  </div>
                )}

                {provisionStep === 1 && (
                  <div className="grid gap-4 md:grid-cols-2">
                    <div className="space-y-2 md:col-span-2">
                      <label className="text-sm font-medium">{t('platformTenants.fields.primaryDomain')}</label>
                      <Input value={form.dominioPrimario} onChange={(e) => handleFormChange('dominioPrimario', e.target.value)} placeholder={t('platformTenants.placeholders.primaryDomain')} />
                    </div>
                    <div className="space-y-2">
                      <label className="text-sm font-medium">{t('platformTenants.fields.primaryColor')}</label>
                      <Input value={form.corPrimaria} onChange={(e) => handleFormChange('corPrimaria', e.target.value)} placeholder="#111827" />
                      {provisionErrors.corPrimaria && <p className="text-xs text-destructive">{provisionErrors.corPrimaria}</p>}
                    </div>
                    <div className="space-y-2">
                      <label className="text-sm font-medium">{t('platformTenants.fields.secondaryColor')}</label>
                      <Input value={form.corSecundaria} onChange={(e) => handleFormChange('corSecundaria', e.target.value)} placeholder="#374151" />
                      {provisionErrors.corSecundaria && <p className="text-xs text-destructive">{provisionErrors.corSecundaria}</p>}
                    </div>
                    <div className="space-y-2 md:col-span-2">
                      <label className="text-sm font-medium">{t('platformTenants.fields.logo')}</label>
                      <div className="flex items-center gap-4">
                        {form.logoUrl ? (
                          <img
                            src={form.logoUrl}
                            alt={t('platformTenants.alt.logo')}
                            className="h-20 w-20 rounded-lg border object-contain bg-white p-2"
                          />
                        ) : (
                          <div className="flex h-20 w-20 items-center justify-center rounded-lg border border-dashed text-xs text-muted-foreground">
                            {t('platformTenants.logoUpload.noLogo')}
                          </div>
                        )}
                        <div className="flex-1 space-y-2">
                          <Input value={form.logoUrl} onChange={(e) => handleFormChange('logoUrl', e.target.value)} placeholder={t('platformTenants.placeholders.logoUrl')} />
                          <Input type="file" accept="image/*" onChange={handleLogoUpload} disabled={uploadingLogo} />
                          <p className="text-xs text-muted-foreground">
                            {uploadingLogo ? t('platformTenants.logoUpload.uploading') : t('platformTenants.logoUpload.hint')}
                          </p>
                        </div>
                      </div>
                    </div>
                    <div className="space-y-2 md:col-span-2">
                      <label className="text-sm font-medium">{t('platformTenants.fields.favicon')}</label>
                      <Input value={form.faviconUrl} onChange={(e) => handleFormChange('faviconUrl', e.target.value)} placeholder={t('platformTenants.placeholders.faviconUrl')} />
                    </div>
                    <div className="rounded-xl border bg-muted/30 p-4 md:col-span-2">
                      <div className="mb-3 flex items-center gap-2 text-sm font-medium">
                        <Palette className="h-4 w-4" />
                        {t('platformTenants.preview.title')}
                      </div>
                      <div className="flex items-center gap-4">
                        <div
                          className="flex h-12 w-12 items-center justify-center rounded-xl border text-white"
                          style={{ background: form.corPrimaria || '#111827' }}
                        >
                          <Shield className="h-5 w-5" />
                        </div>
                        <div className="space-y-1">
                          <div className="font-semibold">{form.nomeExibicao || form.nome || t('platformTenants.preview.defaultName')}</div>
                          <div className="text-sm text-muted-foreground">{form.slug || t('platformTenants.preview.defaultSlug')}</div>
                          <div className="flex gap-2">
                            <span className="rounded-full px-2 py-0.5 text-xs text-white" style={{ background: form.corPrimaria || '#111827' }}>
                              {t('platformTenants.preview.primary')}
                            </span>
                            <span className="rounded-full px-2 py-0.5 text-xs text-white" style={{ background: form.corSecundaria || '#374151' }}>
                              {t('platformTenants.preview.secondary')}
                            </span>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                )}

                {provisionStep === 2 && (
                  <div className="grid gap-4 md:grid-cols-2">
                    <div className="space-y-2">
                      <label className="text-sm font-medium">{t('platformTenants.fields.adminName')}</label>
                      <Input value={form.adminNome} onChange={(e) => handleFormChange('adminNome', e.target.value)} placeholder={t('platformTenants.placeholders.adminName')} />
                      {provisionErrors.adminNome && <p className="text-xs text-destructive">{provisionErrors.adminNome}</p>}
                    </div>
                    <div className="space-y-2">
                      <label className="text-sm font-medium">{t('platformTenants.fields.adminEmail')}</label>
                      <Input value={form.adminEmail} onChange={(e) => handleFormChange('adminEmail', e.target.value)} placeholder={t('platformTenants.placeholders.email')} />
                      {provisionErrors.adminEmail && <p className="text-xs text-destructive">{provisionErrors.adminEmail}</p>}
                    </div>
                    <div className="space-y-2">
                      <label className="text-sm font-medium">{t('platformTenants.fields.phone')}</label>
                      <Input value={form.adminTelefone} onChange={(e) => handleFormChange('adminTelefone', e.target.value)} placeholder={t('platformTenants.placeholders.phone')} />
                    </div>
                    <div className="space-y-2">
                      <label className="text-sm font-medium">{t('platformTenants.fields.whatsApp')}</label>
                      <Input value={form.adminWhatsApp} onChange={(e) => handleFormChange('adminWhatsApp', e.target.value)} placeholder={t('platformTenants.placeholders.whatsApp')} />
                    </div>
                    <div className="space-y-2">
                      <label className="text-sm font-medium">{t('platformTenants.fields.loginEmail')}</label>
                      <Input value={form.adminEmailLogin} onChange={(e) => handleFormChange('adminEmailLogin', e.target.value)} placeholder={t('platformTenants.placeholders.email')} />
                      {provisionErrors.adminEmailLogin && <p className="text-xs text-destructive">{provisionErrors.adminEmailLogin}</p>}
                    </div>
                    <div className="space-y-2">
                      <label className="text-sm font-medium">{t('platformTenants.fields.initialPassword')}</label>
                      <Input type="password" value={form.adminSenha} onChange={(e) => handleFormChange('adminSenha', e.target.value)} placeholder={t('platformTenants.placeholders.password')} />
                      {provisionErrors.adminSenha && <p className="text-xs text-destructive">{provisionErrors.adminSenha}</p>}
                    </div>
                  </div>
                )}

                {provisionStep === 3 && (
                  <div className="space-y-4">
                    <div className="grid gap-4 md:grid-cols-2">
                      <Card>
                        <CardHeader className="pb-2">
                          <CardTitle className="text-sm">{t('platformTenants.review.tenantCard')}</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-1 text-sm">
                          <div><span className="text-muted-foreground">{t('platformTenants.fields.name')}:</span> {form.nome || '-'}</div>
                          <div><span className="text-muted-foreground">{t('platformTenants.fields.displayName')}:</span> {form.nomeExibicao || '-'}</div>
                          <div><span className="text-muted-foreground">{t('platformTenants.fields.slug')}:</span> {form.slug || '-'}</div>
                          <div><span className="text-muted-foreground">{t('platformTenants.fields.domain')}:</span> {form.dominioPrimario || '-'}</div>
                        </CardContent>
                      </Card>
                      <Card>
                        <CardHeader className="pb-2">
                          <CardTitle className="text-sm">{t('platformTenants.review.adminCard')}</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-1 text-sm">
                          <div><span className="text-muted-foreground">{t('platformTenants.fields.name')}:</span> {form.adminNome || '-'}</div>
                          <div><span className="text-muted-foreground">{t('platformTenants.fields.email')}:</span> {form.adminEmail || '-'}</div>
                          <div><span className="text-muted-foreground">{t('platformTenants.fields.login')}:</span> {form.adminEmailLogin || '-'}</div>
                          <div><span className="text-muted-foreground">{t('platformTenants.fields.phone')}:</span> {form.adminTelefone || '-'}</div>
                        </CardContent>
                      </Card>
                    </div>
                    <div className="rounded-xl border bg-muted/30 p-4 text-sm text-muted-foreground">
                      {t('platformTenants.review.description')}
                    </div>
                  </div>
                )}
              </div>
            </>
          )}

          <DialogFooter>
            {provisionResult ? (
              <div className="flex w-full flex-col gap-2 sm:flex-row sm:justify-end">
                {provisionResult.tenant?.id && (
                  <Button variant="outline" asChild>
                    <Link to={`/plataforma/tenants/${provisionResult.tenant.id}`}>
                      {t('platformTenants.provision.result.openDetails')}
                    </Link>
                  </Button>
                )}
                <Button onClick={() => handleProvisionDialogChange(false)}>
                  {t('platformTenants.actions.close')}
                </Button>
              </div>
            ) : (
              <>
                <Button variant="outline" onClick={() => handleProvisionDialogChange(false)} disabled={saving}>
                  {t('actions.cancel')}
                </Button>
                {provisionStep > 0 && (
                  <Button variant="outline" onClick={handleProvisionPreviousStep} disabled={saving}>
                    {t('actions.back')}
                  </Button>
                )}
                {provisionStep < provisionSteps.length - 1 ? (
                  <Button onClick={handleProvisionNextStep} disabled={saving || uploadingLogo}>
                    {t('platformTenants.actions.nextStep')}
                  </Button>
                ) : (
                  <Button onClick={handleProvision} disabled={saving || uploadingLogo}>
                    {saving ? t('platformTenants.provision.provisioning') : t('platformTenants.provision.action')}
                  </Button>
                )}
              </>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={showEditDialog} onOpenChange={setShowEditDialog}>
        <DialogContent className="sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>{t('platformTenants.edit.title')}</DialogTitle>
            <DialogDescription>
              {t('platformTenants.edit.description')}
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('platformTenants.fields.churchName')}</label>
              <Input value={form.nome} onChange={(e) => handleFormChange('nome', e.target.value)} placeholder={t('platformTenants.placeholders.churchName')} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('platformTenants.fields.displayName')}</label>
              <Input value={form.nomeExibicao} onChange={(e) => handleFormChange('nomeExibicao', e.target.value)} placeholder={t('platformTenants.placeholders.displayNameEdit')} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('platformTenants.fields.slug')}</label>
              <Input value={form.slug} onChange={(e) => handleFormChange('slug', e.target.value)} placeholder={t('platformTenants.placeholders.slug')} />
            </div>
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">{t('platformTenants.fields.primaryDomain')}</label>
              <Input value={form.dominioPrimario} onChange={(e) => handleFormChange('dominioPrimario', e.target.value)} placeholder={t('platformTenants.placeholders.primaryDomain')} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('platformTenants.fields.primaryColor')}</label>
              <Input value={form.corPrimaria} onChange={(e) => handleFormChange('corPrimaria', e.target.value)} placeholder="#111827" />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('platformTenants.fields.secondaryColor')}</label>
              <Input value={form.corSecundaria} onChange={(e) => handleFormChange('corSecundaria', e.target.value)} placeholder="#374151" />
            </div>
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">{t('platformTenants.fields.logo')}</label>
              <div className="flex items-center gap-4">
                {form.logoUrl ? (
                  <img
                    src={form.logoUrl}
                    alt={t('platformTenants.alt.logo')}
                    className="h-20 w-20 rounded-lg border object-contain bg-white p-2"
                  />
                ) : (
                  <div className="flex h-20 w-20 items-center justify-center rounded-lg border border-dashed text-xs text-muted-foreground">
                    {t('platformTenants.logoUpload.noLogo')}
                  </div>
                )}
                <div className="flex-1 space-y-2">
                  <Input value={form.logoUrl} onChange={(e) => handleFormChange('logoUrl', e.target.value)} placeholder={t('platformTenants.placeholders.logoUrl')} />
                  <Input type="file" accept="image/*" onChange={handleLogoUpload} disabled={uploadingLogo} />
                  <p className="text-xs text-muted-foreground">
                    {uploadingLogo ? t('platformTenants.logoUpload.uploading') : t('platformTenants.logoUpload.hint')}
                  </p>
                </div>
              </div>
            </div>
            <div className="space-y-2 md:col-span-2">
              <label className="text-sm font-medium">{t('platformTenants.fields.favicon')}</label>
              <Input value={form.faviconUrl} onChange={(e) => handleFormChange('faviconUrl', e.target.value)} placeholder={t('platformTenants.placeholders.faviconUrl')} />
            </div>
            <div className="rounded-xl border bg-muted/30 p-4 md:col-span-2">
              <div className="mb-3 flex items-center gap-2 text-sm font-medium">
                <Palette className="h-4 w-4" />
                {t('platformTenants.preview.title')}
              </div>
              <div className="flex items-center gap-4">
                <div
                  className="flex h-12 w-12 items-center justify-center rounded-xl border text-white"
                  style={{ background: form.corPrimaria || '#111827' }}
                >
                  <Shield className="h-5 w-5" />
                </div>
                <div className="space-y-1">
                  <div className="font-semibold">{form.nomeExibicao || form.nome || t('platformTenants.preview.defaultName')}</div>
                  <div className="text-sm text-muted-foreground">{form.slug || t('platformTenants.preview.defaultSlug')}</div>
                  <div className="flex gap-2">
                    <span className="rounded-full px-2 py-0.5 text-xs text-white" style={{ background: form.corPrimaria || '#111827' }}>
                      {t('platformTenants.preview.primary')}
                    </span>
                    <span className="rounded-full px-2 py-0.5 text-xs text-white" style={{ background: form.corSecundaria || '#374151' }}>
                      {t('platformTenants.preview.secondary')}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setShowEditDialog(false)} disabled={saving}>
              {t('actions.cancel')}
            </Button>
            <Button onClick={handleUpdate} disabled={saving || uploadingLogo}>
              {saving ? t('actions.saving') : t('platformTenants.edit.save')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={confirmDialog.open}
        onOpenChange={(open) => {
          if (!open) {
            confirmDialog.hide();
          }
        }}
        title={confirmDialog.config.title}
        description={confirmDialog.config.description}
        confirmText={confirmDialog.config.confirmText}
        cancelText={confirmDialog.config.cancelText}
        variant={confirmDialog.config.variant}
        onConfirm={confirmDialog.handleConfirm}
        loading={confirmDialog.loading}
      />
    </div>
  );
}
