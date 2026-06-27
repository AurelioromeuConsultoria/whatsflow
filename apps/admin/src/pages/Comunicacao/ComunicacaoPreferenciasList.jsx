import React, { useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { ArrowRightLeft, History, RefreshCcw, Save, Search, ShieldCheck, UserRound } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { comunicacaoAutomacoesApi, comunicacaoPreferenciasApi, pessoasApi } from '@/lib/api';
import { getApiErrorMessage } from '@/lib/apiError';
import { formatDateTime } from '@/lib/formatters';
import { toast } from 'sonner';

const gatilhoLabel = (gatilho, t) => {
  switch (gatilho) {
    case 'ExecutarNovoVisitante': return t('communicationPreferences.triggers.newVisitor');
    case 'ExecutarAniversariosDoDia': return t('communicationPreferences.triggers.birthday');
    case 'ExecutarLembreteOperacional': return t('communicationPreferences.triggers.operationalReminder');
    case 'ExecutarAvisoContextualKids': return t('communicationPreferences.triggers.kidsContextual');
    default: return gatilho;
  }
};

export default function ComunicacaoPreferenciasList() {
  const { t } = useTranslation();
  const canais = [
    { value: 1, label: t('communicationPreferences.channels.whatsapp') },
    { value: 2, label: t('communicationPreferences.channels.email') },
    { value: 3, label: t('communicationPreferences.channels.push') },
    { value: 4, label: t('communicationPreferences.channels.internalNotification') },
  ];
  const [pessoaId, setPessoaId] = useState('');
  const [pessoaQuery, setPessoaQuery] = useState('');
  const [pessoaResultados, setPessoaResultados] = useState([]);
  const [searchingPessoas, setSearchingPessoas] = useState(false);
  const [selectedPessoa, setSelectedPessoa] = useState(null);
  const [preferencias, setPreferencias] = useState([]);
  const [loadingPreferencias, setLoadingPreferencias] = useState(false);
  const [savingCanal, setSavingCanal] = useState(null);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [historyFilter, setHistoryFilter] = useState({ gatilho: '', chaveEvento: '' });
  const [history, setHistory] = useState([]);

  const pessoaSelecionadaDescricao = useMemo(() => {
    if (!selectedPessoa) return null;

    return [
      selectedPessoa.nome,
      selectedPessoa.email,
      selectedPessoa.whatsApp || selectedPessoa.telefone,
    ]
      .filter(Boolean)
      .join(' • ');
  }, [selectedPessoa]);

  const buscarPessoas = useCallback(async () => {
    const query = pessoaQuery.trim();
    if (query.length < 2) {
      toast.error(t('communicationPreferences.search.minChars'));
      return;
    }

    try {
      setSearchingPessoas(true);
      const queryDigits = query.replace(/\D/g, '');
      const looksLikeEmail = query.includes('@');
      const looksLikePhone = queryDigits.length >= 8;
      const requests = looksLikePhone
        ? [
            pessoasApi.getPaged({
              page: 1,
              pageSize: 8,
              sort: 'nome',
              direction: 'asc',
              telefone: queryDigits,
            }),
            pessoasApi.getPaged({
              page: 1,
              pageSize: 8,
              sort: 'nome',
              direction: 'asc',
              whatsApp: queryDigits,
            }),
          ]
        : [
            pessoasApi.getPaged({
              page: 1,
              pageSize: 8,
              sort: 'nome',
              direction: 'asc',
              nome: !looksLikeEmail ? query : undefined,
              email: looksLikeEmail ? query : undefined,
            }),
          ];

      const responses = await Promise.all(requests);
      const merged = responses.flatMap((response) => response.data?.items || []);
      const deduped = Array.from(new Map(merged.map((item) => [item.id, item])).values());
      setPessoaResultados(deduped.slice(0, 8));
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('communicationPreferences.search.error')));
    } finally {
      setSearchingPessoas(false);
    }
  }, [pessoaQuery, t]);

  const selecionarPessoa = useCallback((pessoa) => {
    setSelectedPessoa(pessoa);
    setPessoaId(String(pessoa.id));
    setPessoaResultados([]);
    setPessoaQuery(pessoa.nome || String(pessoa.id));
  }, []);

  const loadPreferencias = useCallback(async () => {
    if (!pessoaId.trim()) {
      toast.error(t('communicationPreferences.preferences.selectPersonFirst'));
      return;
    }

    try {
      setLoadingPreferencias(true);
      const response = await comunicacaoPreferenciasApi.getByPessoaId(pessoaId.trim());
      const items = response.data || [];
      const mapped = canais.map((canal) => items.find((item) => Number(item.canal) === canal.value) || {
        id: null,
        pessoaId: Number(pessoaId),
        canal: canal.value,
        status: 1,
        origemConsentimento: null,
        dataCriacao: null,
        dataAtualizacao: null,
      });
      setPreferencias(mapped);
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('communicationPreferences.preferences.loadError')));
    } finally {
      setLoadingPreferencias(false);
    }
  }, [pessoaId, t, canais]);

  const loadHistorico = useCallback(async () => {
    try {
      setHistoryLoading(true);
      const response = await comunicacaoAutomacoesApi.getHistorico({
        page: 1,
        pageSize: 20,
        gatilho: historyFilter.gatilho || undefined,
        chaveEvento: historyFilter.chaveEvento || undefined,
      });
      setHistory(response.data?.items || []);
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('communicationPreferences.history.loadError')));
    } finally {
      setHistoryLoading(false);
    }
  }, [historyFilter, t]);

  const salvarPreferencia = async (canal, status, origemConsentimento) => {
    if (!pessoaId.trim()) {
      toast.error(t('communicationPreferences.preferences.selectPersonBeforeSave'));
      return;
    }

    try {
      setSavingCanal(canal);
      const response = await comunicacaoPreferenciasApi.update(pessoaId.trim(), canal, {
        status,
        origemConsentimento: origemConsentimento || null,
      });
      setPreferencias((prev) => prev.map((item) => (
        Number(item.canal) === Number(canal) ? response.data : item
      )));
      toast.success(t('communicationPreferences.preferences.saveSuccess'));
    } catch (err) {
      toast.error(getApiErrorMessage(err, t('communicationPreferences.preferences.saveError')));
    } finally {
      setSavingCanal(null);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold text-foreground">{t('communicationPreferences.title')}</h1>
          <p className="text-muted-foreground mt-1">{t('communicationPreferences.subtitle')}</p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" asChild>
            <Link to="/comunicacao/campanhas">{t('communicationPreferences.actions.campaigns')}</Link>
          </Button>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <ShieldCheck className="w-5 h-5" />
            {t('communicationPreferences.preferences.title')}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 lg:grid-cols-[minmax(0,1.2fr)_220px_auto] gap-3 items-end">
            <div className="space-y-2">
              <Label htmlFor="pessoaQuery">{t('communicationPreferences.search.person')}</Label>
              <Input
                id="pessoaQuery"
                value={pessoaQuery}
                onChange={(e) => setPessoaQuery(e.target.value)}
                placeholder={t('communicationPreferences.search.placeholder')}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="pessoaId">{t('communicationPreferences.search.personId')}</Label>
              <Input
                id="pessoaId"
                value={pessoaId}
                onChange={(e) => {
                  setPessoaId(e.target.value);
                  setSelectedPessoa(null);
                }}
                placeholder={t('communicationPreferences.search.personIdPlaceholder')}
              />
            </div>
            <Button variant="outline" onClick={buscarPessoas} disabled={searchingPessoas}>
              <Search className="w-4 h-4 mr-2" />
              {searchingPessoas ? t('communicationPreferences.search.searching') : t('communicationPreferences.search.action')}
            </Button>
          </div>

          {!!pessoaResultados.length && (
            <div className="rounded-lg border border-border divide-y divide-border overflow-hidden">
              {pessoaResultados.map((pessoa) => (
                <button
                  key={pessoa.id}
                  type="button"
                  onClick={() => selecionarPessoa(pessoa)}
                  className="w-full text-left px-4 py-3 hover:bg-muted/50 transition-colors"
                >
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <div className="font-medium text-foreground">{pessoa.nome}</div>
                      <div className="text-sm text-muted-foreground">
                        {[pessoa.email, pessoa.whatsApp || pessoa.telefone].filter(Boolean).join(' • ') || t('communicationPreferences.search.noPrimaryContact')}
                      </div>
                    </div>
                    <Badge variant="outline">{t('communicationPreferences.search.idBadge', { id: pessoa.id })}</Badge>
                  </div>
                </button>
              ))}
            </div>
          )}

          {!!selectedPessoa && (
            <div className="rounded-lg border border-border bg-muted/30 px-4 py-3 flex flex-col md:flex-row md:items-center md:justify-between gap-3">
              <div className="flex items-start gap-3">
                <UserRound className="w-5 h-5 mt-0.5 text-muted-foreground" />
                <div>
                  <div className="font-medium">{t('communicationPreferences.search.selectedPerson')}</div>
                  <div className="text-sm text-muted-foreground">{pessoaSelecionadaDescricao}</div>
                </div>
              </div>
              <Badge variant="secondary">{t('communicationPreferences.search.personIdBadge', { id: selectedPessoa.id })}</Badge>
            </div>
          )}

          <div className="flex justify-end">
            <Button onClick={loadPreferencias} disabled={loadingPreferencias}>
              <RefreshCcw className="w-4 h-4 mr-2" />
              {loadingPreferencias ? t('communicationPreferences.preferences.loading') : t('communicationPreferences.preferences.loadAction')}
            </Button>
          </div>

          <div className="space-y-3">
            {preferencias.map((item) => (
              <div key={item.canal} className="rounded-lg border border-border p-4 space-y-3">
                <div className="flex items-center justify-between gap-3">
                  <div className="font-medium">{canais.find((canal) => canal.value === Number(item.canal))?.label}</div>
                  <Badge variant={Number(item.status) === 2 ? 'destructive' : 'outline'}>
                    {Number(item.status) === 2 ? t('communicationPreferences.preferences.status.blocked') : t('communicationPreferences.preferences.status.allowed')}
                  </Badge>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-[180px_1fr_auto] gap-3 items-end">
                  <div className="space-y-2">
                    <Label>{t('communicationPreferences.preferences.fields.status')}</Label>
                    <select
                      value={item.status}
                      onChange={(e) => setPreferencias((prev) => prev.map((pref) => Number(pref.canal) === Number(item.canal) ? { ...pref, status: Number(e.target.value) } : pref))}
                      className="w-full rounded-md border border-input bg-background px-3 py-2"
                    >
                      <option value={1}>{t('communicationPreferences.preferences.status.allowed')}</option>
                      <option value={2}>{t('communicationPreferences.preferences.status.blocked')}</option>
                    </select>
                  </div>
                  <div className="space-y-2">
                    <Label>{t('communicationPreferences.preferences.fields.consentOrigin')}</Label>
                    <Input
                      value={item.origemConsentimento || ''}
                      onChange={(e) => setPreferencias((prev) => prev.map((pref) => Number(pref.canal) === Number(item.canal) ? { ...pref, origemConsentimento: e.target.value } : pref))}
                      placeholder={t('communicationPreferences.preferences.fields.consentOriginPlaceholder')}
                    />
                  </div>
                  <Button
                    onClick={() => salvarPreferencia(item.canal, item.status, item.origemConsentimento)}
                    disabled={savingCanal === item.canal}
                  >
                    <Save className="w-4 h-4 mr-2" />
                    {savingCanal === item.canal ? t('actions.saving') : t('actions.save')}
                  </Button>
                </div>
              </div>
            ))}
            {!preferencias.length && (
              <div className="text-sm text-muted-foreground rounded-lg border border-dashed border-border p-6">
                {t('communicationPreferences.preferences.empty')}
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <History className="w-5 h-5" />
            {t('communicationPreferences.history.title')}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-[220px_1fr_auto] gap-3 items-end">
            <div className="space-y-2">
              <Label>{t('communicationPreferences.history.fields.trigger')}</Label>
              <select
                value={historyFilter.gatilho}
                onChange={(e) => setHistoryFilter((prev) => ({ ...prev, gatilho: e.target.value }))}
                className="w-full rounded-md border border-input bg-background px-3 py-2"
              >
                <option value="">{t('communicationPreferences.history.all')}</option>
                <option value="ExecutarNovoVisitante">{t('communicationPreferences.triggers.newVisitor')}</option>
                <option value="ExecutarAniversariosDoDia">{t('communicationPreferences.triggers.birthday')}</option>
                <option value="ExecutarLembreteOperacional">{t('communicationPreferences.triggers.operationalReminder')}</option>
                <option value="ExecutarAvisoContextualKids">{t('communicationPreferences.triggers.kidsContextual')}</option>
              </select>
            </div>
            <div className="space-y-2">
              <Label>{t('communicationPreferences.history.fields.eventKey')}</Label>
              <Input
                value={historyFilter.chaveEvento}
                onChange={(e) => setHistoryFilter((prev) => ({ ...prev, chaveEvento: e.target.value }))}
                placeholder={t('communicationPreferences.history.fields.eventKeyPlaceholder')}
              />
            </div>
            <Button onClick={loadHistorico} disabled={historyLoading}>
              <ArrowRightLeft className="w-4 h-4 mr-2" />
              {historyLoading ? t('communicationPreferences.history.loading') : t('communicationPreferences.history.loadAction')}
            </Button>
          </div>

          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('communicationPreferences.history.table.trigger')}</TableHead>
                <TableHead>{t('communicationPreferences.history.table.key')}</TableHead>
                <TableHead>{t('communicationPreferences.history.table.executedAt')}</TableHead>
                <TableHead>{t('communicationPreferences.history.table.payload')}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {history.map((item) => (
                <TableRow key={item.id}>
                  <TableCell>{gatilhoLabel(item.gatilho, t)}</TableCell>
                  <TableCell className="font-mono text-xs">{item.chaveEvento}</TableCell>
                  <TableCell>{formatDateTime(item.executadoEm)}</TableCell>
                  <TableCell className="whitespace-normal break-all text-xs text-muted-foreground">{item.payloadJson || '-'}</TableCell>
                </TableRow>
              ))}
              {history.length === 0 && (
                <TableRow>
                  <TableCell colSpan={4} className="text-center text-muted-foreground py-8">
                    {t('communicationPreferences.history.empty')}
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
