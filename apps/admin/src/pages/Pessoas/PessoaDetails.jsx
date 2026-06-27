import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { ArrowLeft, Edit, Phone, Mail, Plus, X, UserPlus, Users, CalendarClock, LogIn, Download, ShieldOff } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { pessoasApi, pessoasPerfisApi, visitantesApi } from '@/lib/api';
import { getApiErrorMessage } from '@/lib/apiError';
import { formatDate, formatDateTime } from '@/lib/formatters';
import { useAuth } from '@/context/AuthContext';
import { RESOURCES, ACTIONS } from '@/utils/permissions';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

const PERFIL_OPTIONS = [
  { value: 'Visitante', labelKey: 'visitor' },
  { value: 'Membro', labelKey: 'member' },
  { value: 'Voluntario', labelKey: 'volunteer' },
  { value: 'Lider', labelKey: 'leader' },
  { value: 'Pastor', labelKey: 'pastor' },
];

export default function PessoaDetails() {
  const { id } = useParams();
  const [dados360, setDados360] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showAddPerfil, setShowAddPerfil] = useState(false);
  const [showAddVisita, setShowAddVisita] = useState(false);
  const [saving, setSaving] = useState(false);
  const { can } = useAuth();
  const confirmDialog = useConfirmDialog();
  const { t } = useTranslation();

  // Formulário de perfil
  const [perfilForm, setPerfilForm] = useState({
    perfil: '',
    dataInicio: new Date().toISOString().split('T')[0],
  });

  // Formulário de visita
  const [visitaForm, setVisitaForm] = useState({
    dataVisita: new Date().toISOString().split('T')[0],
    observacoes: '',
  });

  const perfilOptions = PERFIL_OPTIONS.map((option) => ({
    value: option.value,
    label: t(`people.details.profileOptions.${option.labelKey}`),
  }));

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await pessoasApi.get360(id);
      setDados360(response.data);
    } catch (err) {
      setError(t('people.details.errorLoad'));
      console.error('Erro ao carregar dados:', err);
      toast.error(t('people.details.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, [id]);

  const handleAddPerfil = async () => {
    if (!perfilForm.perfil) {
      toast.error(t('people.details.selectProfile'));
      return;
    }

    try {
      setSaving(true);
      await pessoasPerfisApi.create({
        pessoaId: parseInt(id),
        perfil: perfilForm.perfil,
        dataInicio: new Date(perfilForm.dataInicio + 'T00:00:00').toISOString(),
      });
      toast.success(t('people.details.profileAdded'));
      setShowAddPerfil(false);
      setPerfilForm({ perfil: '', dataInicio: new Date().toISOString().split('T')[0] });
      await loadData();
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('people.details.profileAddError')));
      console.error('Erro ao adicionar perfil:', err);
    } finally {
      setSaving(false);
    }
  };

  const handleEncerrarPerfil = async (perfilId) => {
    const perfis = dados360?.pessoa?.perfis ?? [];
    const perfil = perfis.find((p) => p.id === perfilId);
    confirmDialog.show({
      title: t('people.details.endProfileTitle'),
      description: t('people.details.endProfileDescription', {
        profile: perfil?.perfil || t('people.details.fallbackProfile'),
      }),
      confirmText: t('people.details.endProfileConfirm'),
      cancelText: t('actions.cancel'),
      variant: 'default',
      onConfirm: async () => {
        try {
          await pessoasPerfisApi.update(perfilId, {
            dataFim: new Date().toISOString(),
          });
          toast.success(t('people.details.endProfileSuccess'));
          await loadData();
        } catch (err) {
          toast.error(getApiErrorMessage(err, t('people.details.endProfileError')));
          console.error('Erro ao encerrar perfil:', err);
          throw err;
        }
      },
    });
  };

  const handleRemoverPerfil = async (perfilId) => {
    const perfis = dados360?.pessoa?.perfis ?? [];
    const perfil = perfis.find((p) => p.id === perfilId);
    confirmDialog.show({
      title: t('people.details.removeProfileTitle'),
      description: t('people.details.removeProfileDescription', {
        profile: perfil?.perfil || t('people.details.fallbackProfile'),
      }),
      confirmText: t('people.details.removeProfileConfirm'),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await pessoasPerfisApi.delete(perfilId);
          toast.success(t('people.details.removeProfileSuccess'));
          await loadData();
        } catch (err) {
          toast.error(getApiErrorMessage(err, t('people.details.removeProfileError')));
          console.error('Erro ao remover perfil:', err);
          throw err;
        }
      },
    });
  };

  const handleExportarDados = async () => {
    try {
      setSaving(true);
      const response = await pessoasApi.exportarDados(id);
      const blob = new Blob([JSON.stringify(response.data, null, 2)], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `dados-pessoais-${id}.json`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
      toast.success(t('people.lgpd.exportSuccess', { defaultValue: 'Dados pessoais exportados com sucesso.' }));
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('people.lgpd.exportError', { defaultValue: 'Erro ao exportar os dados.' })));
      console.error('Erro ao exportar dados pessoais:', err);
    } finally {
      setSaving(false);
    }
  };

  const handleAnonimizar = () => {
    confirmDialog.show({
      title: t('people.lgpd.anonymizeTitle', { defaultValue: 'Anonimizar dados do titular?' }),
      description: t('people.lgpd.anonymizeDescription', {
        defaultValue:
          'Esta ação remove de forma irreversível os dados pessoais identificáveis (nome, contato, documento e dados de saúde), preservando registros financeiros e de presença de forma anonimizada. Use para atender ao direito ao esquecimento (LGPD).',
      }),
      confirmText: t('people.lgpd.anonymizeConfirm', { defaultValue: 'Anonimizar' }),
      cancelText: t('actions.cancel'),
      variant: 'destructive',
      onConfirm: async () => {
        try {
          await pessoasApi.anonimizar(id);
          toast.success(t('people.lgpd.anonymizeSuccess', { defaultValue: 'Titular anonimizado com sucesso.' }));
          await loadData();
        } catch (err) {
          toast.error(getApiErrorMessage(err, t('people.lgpd.anonymizeError', { defaultValue: 'Erro ao anonimizar o titular.' })));
          console.error('Erro ao anonimizar titular:', err);
          throw err;
        }
      },
    });
  };

  const handleAddVisita = async () => {
    if (!visitaForm.dataVisita || !dados360?.pessoa) {
      toast.error(t('people.details.visitDateRequired'));
      return;
    }

    try {
      setSaving(true);
      await visitantesApi.create({
        nome: dados360.pessoa.nome,
        email: dados360.pessoa.email,
        telefone: dados360.pessoa.telefone,
        whatsApp: dados360.pessoa.whatsApp,
        dataNascimento: dados360.pessoa.dataNascimento,
        dataVisita: new Date(visitaForm.dataVisita + 'T00:00:00').toISOString(),
        observacoes: visitaForm.observacoes || null,
      });
      toast.success(t('people.details.visitAdded'));
      setShowAddVisita(false);
      setVisitaForm({ dataVisita: new Date().toISOString().split('T')[0], observacoes: '' });
      await loadData();
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('people.details.visitAddError')));
      console.error('Erro ao registrar visita:', err);
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <LoadingPage text={t('people.form.loading')} />;
  }

  if (error) {
    return <ErrorPage message={error} onRetry={loadData} />;
  }

  if (!dados360?.pessoa) {
    return <ErrorPage message={t('people.details.notFound')} />;
  }

  const pessoa = dados360.pessoa;
  const perfis = pessoa.perfis ?? [];
  const perfisAtivos = perfis.filter(p => !p.dataFim);
  const perfisHistorico = perfis.filter(p => p.dataFim);
  const visitantes = dados360.visitantes ?? [];
  const voluntarios = dados360.voluntarios ?? [];
  const usuario = dados360.usuario;
  const canCreateUsuario = can(RESOURCES.USUARIOS, ACTIONS.EDIT);
  const canExportarDados = can(RESOURCES.PESSOAS, ACTIONS.VIEW);
  const canAnonimizar = can(RESOURCES.PESSOAS, ACTIONS.DELETE);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <Button variant="ghost" asChild>
            <Link to="/pessoas">
              <ArrowLeft className="h-4 w-4 mr-2" />
              {t('actions.back')}
            </Link>
          </Button>
          <div>
            <h1 className="text-3xl font-bold">{pessoa.nome}</h1>
            <p className="text-muted-foreground">
              {t('people.details.subtitle')}
            </p>
          </div>
        </div>
        <div className="flex items-center space-x-2">
          {canCreateUsuario && !usuario && (
            <Button variant="outline" asChild>
              <Link to={`/usuarios?pessoaId=${id}`}>
                <UserPlus className="h-4 w-4 mr-2" />
                {t('people.details.createAccess')}
              </Link>
            </Button>
          )}
          <Dialog open={showAddVisita} onOpenChange={setShowAddVisita}>
            <DialogTrigger asChild>
              <Button variant="outline">
                <UserPlus className="h-4 w-4 mr-2" />
                {t('people.details.addVisit')}
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>{t('people.details.addVisitDialogTitle')}</DialogTitle>
              </DialogHeader>
              <div className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="dataVisita">{t('visitors.form.fields.visitDate')} *</Label>
                  <Input
                    id="dataVisita"
                    type="date"
                    value={visitaForm.dataVisita}
                    onChange={(e) => setVisitaForm(prev => ({ ...prev, dataVisita: e.target.value }))}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="observacoes">{t('visitors.form.fields.notes')}</Label>
                  <Input
                    id="observacoes"
                    value={visitaForm.observacoes}
                    onChange={(e) => setVisitaForm(prev => ({ ...prev, observacoes: e.target.value }))}
                    placeholder={t('visitors.form.placeholders.notes')}
                  />
                </div>
                <div className="flex justify-end space-x-2">
                  <Button variant="outline" onClick={() => setShowAddVisita(false)}>
                    {t('actions.cancel')}
                  </Button>
                  <Button onClick={handleAddVisita} disabled={saving}>
                    {saving ? t('actions.saving') : t('people.details.registerVisit')}
                  </Button>
                </div>
              </div>
            </DialogContent>
          </Dialog>
          <Button asChild>
            <Link to={`/pessoas/${id}/editar`}>
              <Edit className="h-4 w-4 mr-2" />
              {t('actions.edit')}
            </Link>
          </Button>
          {canExportarDados && (
            <Button variant="outline" onClick={handleExportarDados} disabled={saving}>
              <Download className="h-4 w-4 mr-2" />
              {t('people.lgpd.export', { defaultValue: 'Exportar dados' })}
            </Button>
          )}
          {canAnonimizar && (
            <Button
              variant="outline"
              onClick={handleAnonimizar}
              className="text-destructive hover:text-destructive"
            >
              <ShieldOff className="h-4 w-4 mr-2" />
              {t('people.lgpd.anonymize', { defaultValue: 'Anonimizar' })}
            </Button>
          )}
        </div>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>{t('people.details.personalInfo')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <p className="text-sm font-medium">{t('people.form.fields.name')}</p>
              <p className="text-sm text-muted-foreground">{pessoa.nome}</p>
            </div>
            {pessoa.email && (
              <div className="flex items-center space-x-2">
                <div>
                  <p className="text-sm font-medium">{t('people.form.fields.email')}</p>
                  <p className="text-sm text-muted-foreground">{pessoa.email}</p>
                </div>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => window.open(`mailto:${pessoa.email}`)}
                >
                  <Mail className="h-4 w-4" />
                </Button>
              </div>
            )}
            {pessoa.telefone && (
              <div>
                <p className="text-sm font-medium">{t('people.form.fields.phone')}</p>
                <p className="text-sm text-muted-foreground">{pessoa.telefone}</p>
              </div>
            )}
            {pessoa.whatsApp && (
              <div className="flex items-center space-x-2">
                <div>
                  <p className="text-sm font-medium">{t('people.form.fields.whatsapp')}</p>
                  <p className="text-sm text-muted-foreground">{pessoa.whatsApp}</p>
                </div>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => window.open(`https://wa.me/55${pessoa.whatsApp.replace(/\D/g, '')}`)}
                >
                  <Phone className="h-4 w-4" />
                </Button>
              </div>
            )}
            {pessoa.dataNascimento && (
              <div>
                <p className="text-sm font-medium">{t('people.form.fields.birthDate')}</p>
                <p className="text-sm text-muted-foreground">
                  {formatDate(pessoa.dataNascimento)}
                </p>
              </div>
            )}
            <div>
              <p className="text-sm font-medium">{t('people.form.fields.personType')}</p>
              <Badge variant="outline">{pessoa.tipoPessoa || '-'}</Badge>
            </div>
            <div>
              <p className="text-sm font-medium">{t('people.details.status')}</p>
              <Badge variant={pessoa.ativo ? 'default' : 'secondary'}>
                {pessoa.ativo ? t('people.status.active') : t('people.status.inactive')}
              </Badge>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center justify-between">
              <span>{t('people.details.profiles')}</span>
              <Dialog open={showAddPerfil} onOpenChange={setShowAddPerfil}>
                <DialogTrigger asChild>
                  <Button size="sm">
                    <Plus className="h-4 w-4 mr-2" />
                    {t('people.details.addProfile')}
                  </Button>
                </DialogTrigger>
                <DialogContent>
                  <DialogHeader>
                    <DialogTitle>{t('people.details.addProfileTitle')}</DialogTitle>
                  </DialogHeader>
                  <div className="space-y-4">
                    <div className="space-y-2">
                      <Label htmlFor="perfil">{t('people.filters.profile')} *</Label>
                      <Select
                        value={perfilForm.perfil}
                        onValueChange={(value) => setPerfilForm(prev => ({ ...prev, perfil: value }))}
                      >
                        <SelectTrigger>
                          <SelectValue placeholder={t('people.details.selectProfilePlaceholder')} />
                        </SelectTrigger>
                        <SelectContent>
                          {perfilOptions.map((option) => (
                            <SelectItem key={option.value} value={option.value}>{option.label}</SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="dataInicio">{t('people.details.startDate')}</Label>
                      <Input
                        id="dataInicio"
                        type="date"
                        value={perfilForm.dataInicio}
                        onChange={(e) => setPerfilForm(prev => ({ ...prev, dataInicio: e.target.value }))}
                      />
                    </div>
                    <div className="flex justify-end space-x-2">
                      <Button variant="outline" onClick={() => setShowAddPerfil(false)}>
                        {t('actions.cancel')}
                      </Button>
                      <Button onClick={handleAddPerfil} disabled={saving}>
                        {saving ? t('actions.saving') : t('people.details.add')}
                      </Button>
                    </div>
                  </div>
                </DialogContent>
              </Dialog>
            </CardTitle>
          </CardHeader>
          <CardContent>
            {perfisAtivos.length > 0 && (
              <div className="space-y-4">
                <div>
                  <p className="text-sm font-medium mb-2">{t('people.details.activeProfiles')}</p>
                  <div className="space-y-2">
                    {perfisAtivos.map((perfil) => (
                      <div key={perfil.id} className="flex items-center justify-between p-2 border rounded">
                        <div>
                          <Badge variant="default">{perfil.perfil}</Badge>
                          <p className="text-xs text-muted-foreground mt-1">
                            {t('people.details.since', { date: formatDate(perfil.dataInicio) })}
                          </p>
                        </div>
                        <div className="flex space-x-1">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleEncerrarPerfil(perfil.id)}
                          >
                            <X className="h-4 w-4" />
                          </Button>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            )}

            {perfisHistorico.length > 0 && (
              <div className="mt-4">
                <p className="text-sm font-medium mb-2">{t('people.details.profileHistory')}</p>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>{t('people.filters.profile')}</TableHead>
                      <TableHead>{t('people.details.startDate')}</TableHead>
                      <TableHead>{t('people.details.endDate')}</TableHead>
                      <TableHead className="text-right">{t('people.table.actions')}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {perfisHistorico.map((perfil) => (
                      <TableRow key={perfil.id}>
                        <TableCell>
                          <Badge variant="secondary">{perfil.perfil}</Badge>
                        </TableCell>
                        <TableCell>
                          {formatDate(perfil.dataInicio)}
                        </TableCell>
                        <TableCell>
                          {perfil.dataFim 
                            ? formatDate(perfil.dataFim)
                            : '-'}
                        </TableCell>
                        <TableCell className="text-right">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleRemoverPerfil(perfil.id)}
                          >
                            <X className="h-4 w-4" />
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}

            {perfis.length === 0 && (
              <p className="text-sm text-muted-foreground text-center py-4">
                {t('people.details.noProfiles')}
              </p>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Histórico de Visitas */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <CalendarClock className="h-5 w-5" />
            {t('people.details.visitHistory', { count: visitantes.length })}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {visitantes.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-4">
              {t('people.details.noVisits')}
            </p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('people.details.date')}</TableHead>
                  <TableHead>{t('visitors.form.fields.notes')}</TableHead>
                  <TableHead className="text-right">{t('people.table.actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {visitantes.map((v) => (
                  <TableRow key={v.id}>
                    <TableCell>
                      {v.dataVisita ? formatDate(v.dataVisita) : '-'}
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {v.observacoes ? (v.observacoes.length > 60 ? v.observacoes.slice(0, 60) + '...' : v.observacoes) : '-'}
                    </TableCell>
                    <TableCell className="text-right">
                      <Button variant="ghost" size="sm" asChild>
                        <Link to={`/visitantes/${v.id}`}>{t('actions.see')}</Link>
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Voluntariado */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Users className="h-5 w-5" />
            {t('people.details.volunteering', { count: voluntarios.length })}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {voluntarios.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-4">
              {t('people.details.noVolunteerLinks')}
            </p>
          ) : (
            <div className="space-y-2">
              {voluntarios.map((v) => (
                <div key={v.id} className="flex items-center justify-between p-3 border rounded">
                  <div>
                    <span className="font-medium">{v.nomeEquipe}</span>
                    <span className="text-muted-foreground mx-2">—</span>
                    <span>{v.nomeCargo}</span>
                  </div>
                  <Button variant="ghost" size="sm" asChild>
                    <Link to={`/voluntarios/${v.id}/editar`}>{t('actions.edit')}</Link>
                  </Button>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Acesso ao Sistema */}
      {usuario && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <LogIn className="h-5 w-5" />
              {t('people.details.systemAccess')}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium">{usuario.emailLogin}</p>
                <p className="text-xs text-muted-foreground">
                  {usuario.tipoUsuarioDescricao}
                  {usuario.perfilAcessoNome && ` • ${usuario.perfilAcessoNome}`}
                  {usuario.ultimoAcesso && ` • ${t('people.details.lastAccess')}: ${formatDateTime(usuario.ultimoAcesso)}`}
                </p>
              </div>
              <Badge variant={usuario.ativo ? 'default' : 'secondary'}>
                {usuario.ativo ? t('people.status.active') : t('people.status.inactive')}
              </Badge>
            </div>
            {canCreateUsuario && (
              <Button variant="outline" size="sm" asChild>
                <Link to={`/usuarios?pessoaId=${id}`}>
                  {t('people.details.manageUser')}
                </Link>
              </Button>
            )}
          </CardContent>
        </Card>
      )}

      <ConfirmDialog
        open={confirmDialog.open}
        onOpenChange={(open) => {
          if (!open) confirmDialog.hide();
        }}
        onConfirm={confirmDialog.handleConfirm}
        title={confirmDialog.config.title}
        description={confirmDialog.config.description}
        confirmText={confirmDialog.config.confirmText}
        cancelText={confirmDialog.config.cancelText}
        variant={confirmDialog.config.variant}
        loading={confirmDialog.loading}
      />
    </div>
  );
}


