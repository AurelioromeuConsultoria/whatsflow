import { useEffect, useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { ArrowLeft, Save, User, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { receitasApi, contasBancariasApi, centrosCustosApi, projetosApi, categoriasReceitasApi, pessoasApi } from '@/lib/api';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

export default function ReceitaForm() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);
  const { t } = useTranslation();

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [contas, setContas] = useState([]);
  const [centrosCustos, setCentrosCustos] = useState([]);
  const [projetos, setProjetos] = useState([]);
  const [categoriasReceitas, setCategoriasReceitas] = useState([]);

  // busca de membro
  const [pessoaBusca, setPessoaBusca] = useState('');
  const [pessoaSelecionada, setPessoaSelecionada] = useState(null);
  const [resultadosBusca, setResultadosBusca] = useState([]);
  const [buscandoPessoa, setBuscandoPessoa] = useState(false);

  const [formData, setFormData] = useState({
    descricao: '',
    valor: '',
    dataRecebimento: '',
    status: 'Pendente',
    observacoes: '',
    comprovanteUrl: '',
    categoriaReceitaId: '',
    contaBancariaId: '',
    centroCustoId: '',
    projetoId: '',
    pessoaId: null,
    recorrente: false,
    tipoRecorrencia: null,
  });

  const loadDependencies = async () => {
    try {
      const [contasRes, centrosRes, projetosRes, categoriasRes] = await Promise.all([
        contasBancariasApi.getAll(),
        centrosCustosApi.getAll(),
        projetosApi.getAll(),
        categoriasReceitasApi.getAll(),
      ]);
      setContas(contasRes.data || []);
      setCentrosCustos(centrosRes.data || []);
      setProjetos(projetosRes.data || []);
      setCategoriasReceitas(categoriasRes.data || []);
    } catch (err) {
      console.error('Erro ao carregar dependências:', err);
    }
  };

  const load = async () => {
    await loadDependencies();
    if (!isEditing) return;
    try {
      setLoading(true);
      setError(null);
      const res = await receitasApi.getById(id);
      const r = res.data || {};
      setFormData({
        descricao: r.descricao || '',
        valor: r.valor !== undefined ? String(r.valor) : '',
        dataRecebimento: r.dataRecebimento ? new Date(r.dataRecebimento).toISOString().slice(0, 10) : '',
        status: r.status || 'Pendente',
        observacoes: r.observacoes || '',
        comprovanteUrl: r.comprovanteUrl || '',
        categoriaReceitaId: r.categoriaReceitaId ? String(r.categoriaReceitaId) : '',
        contaBancariaId: r.contaBancariaId ? String(r.contaBancariaId) : '',
        centroCustoId: r.centroCustoId ? String(r.centroCustoId) : '',
        projetoId: r.projetoId ? String(r.projetoId) : '',
        pessoaId: r.pessoaId || null,
        recorrente: r.recorrente || false,
        tipoRecorrencia: r.tipoRecorrencia || null,
      });
      if (r.pessoaId && r.pessoaNome) {
        setPessoaSelecionada({ id: r.pessoaId, nome: r.pessoaNome });
      }
    } catch (err) {
      setError(t('finance.revenues.form.saveError'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [id]);

  // Busca de pessoa com debounce
  useEffect(() => {
    if (!pessoaBusca.trim() || pessoaBusca.length < 2) {
      setResultadosBusca([]);
      return;
    }
    const timer = setTimeout(async () => {
      setBuscandoPessoa(true);
      try {
        const res = await pessoasApi.getPaged({ nome: pessoaBusca, pageSize: 8, ativo: true });
        setResultadosBusca(res.data?.items || []);
      } catch {
        setResultadosBusca([]);
      } finally {
        setBuscandoPessoa(false);
      }
    }, 300);
    return () => clearTimeout(timer);
  }, [pessoaBusca]);

  const selecionarPessoa = (pessoa) => {
    setPessoaSelecionada(pessoa);
    setFormData((prev) => ({ ...prev, pessoaId: pessoa.id }));
    setPessoaBusca('');
    setResultadosBusca([]);
  };

  const limparPessoa = () => {
    setPessoaSelecionada(null);
    setFormData((prev) => ({ ...prev, pessoaId: null }));
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.descricao.trim()) {
      toast.error(t('finance.revenues.form.validation.descriptionRequired'));
      return;
    }
    if (!formData.valor) {
      toast.error(t('finance.revenues.form.validation.valueRequired'));
      return;
    }
    if (!formData.dataRecebimento) {
      toast.error(t('finance.revenues.form.validation.dateRequired'));
      return;
    }

    try {
      setLoading(true);
      const payload = {
        descricao: formData.descricao.trim(),
        valor: parseFloat(formData.valor) || 0,
        dataRecebimento: new Date(formData.dataRecebimento).toISOString(),
        status: formData.status,
        observacoes: formData.observacoes?.trim() || null,
        comprovanteUrl: formData.comprovanteUrl?.trim() || null,
        categoriaReceitaId: formData.categoriaReceitaId ? Number(formData.categoriaReceitaId) : null,
        contaBancariaId: formData.contaBancariaId ? Number(formData.contaBancariaId) : null,
        centroCustoId: formData.centroCustoId ? Number(formData.centroCustoId) : null,
        projetoId: formData.projetoId ? Number(formData.projetoId) : null,
        pessoaId: formData.pessoaId || null,
        recorrente: formData.recorrente,
        tipoRecorrencia: formData.recorrente && formData.tipoRecorrencia ? Number(formData.tipoRecorrencia) : null,
      };
      if (isEditing) await receitasApi.update(id, payload);
      else await receitasApi.create(payload);
      toast.success(isEditing ? t('finance.revenues.form.saveSuccessEdit') : t('finance.revenues.form.saveSuccessCreate'));
      navigate('/financeiro/receitas');
    } catch (err) {
      const errorMessage = err.response?.data?.message || t('finance.revenues.form.saveError');
      toast.error(errorMessage);
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading && isEditing) return <LoadingPage text={t('finance.revenues.form.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to="/financeiro/receitas">
            <ArrowLeft className="h-4 w-4 mr-2" /> {t('actions.back')}
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">{isEditing ? t('finance.revenues.form.editTitle') : t('finance.revenues.form.newTitle')}</h1>
          <p className="text-muted-foreground">{isEditing ? t('finance.revenues.form.editSubtitle') : t('finance.revenues.form.newSubtitle')}</p>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>{t('finance.revenues.form.cardTitle')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2 md:col-span-2">
                <Label htmlFor="descricao">{t('finance.revenues.form.fields.description')} *</Label>
                <Input id="descricao" name="descricao" value={formData.descricao} onChange={handleChange} placeholder={t('finance.revenues.form.fields.descriptionPlaceholder')} required />
              </div>
              <div className="space-y-2">
                <Label htmlFor="valor">{t('finance.revenues.form.fields.value')} *</Label>
                <Input id="valor" name="valor" type="number" step="0.01" value={formData.valor} onChange={handleChange} placeholder="0.00" required />
              </div>
              <div className="space-y-2">
                <Label htmlFor="dataRecebimento">{t('finance.revenues.form.fields.date')} *</Label>
                <Input id="dataRecebimento" name="dataRecebimento" type="date" value={formData.dataRecebimento} onChange={handleChange} required />
              </div>
              <div className="space-y-2">
                <Label htmlFor="status">{t('finance.revenues.form.fields.status')} *</Label>
                <select id="status" name="status" value={formData.status} onChange={handleChange} className="w-full px-3 py-2 border rounded" required>
                  <option value="Pendente">{t('finance.revenues.status.pending')}</option>
                  <option value="Recebida">{t('finance.revenues.status.received')}</option>
                  <option value="Cancelada">{t('finance.revenues.status.canceled')}</option>
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="categoriaReceitaId">{t('finance.revenues.form.fields.category')}</Label>
                <select id="categoriaReceitaId" name="categoriaReceitaId" value={formData.categoriaReceitaId} onChange={handleChange} className="w-full px-3 py-2 border rounded">
                  <option value="">{t('actions.select')}</option>
                  {categoriasReceitas.filter(c => c.ativo).map((c) => (
                    <option key={c.id} value={c.id}>{c.nome}</option>
                  ))}
                </select>
              </div>

              {/* Campo de membro/contribuinte */}
              <div className="space-y-2 md:col-span-2">
                <Label className="flex items-center gap-2">
                  <User className="h-4 w-4" />
                  Membro contribuinte <span className="text-muted-foreground text-xs font-normal">(opcional — para rastrear dízimos)</span>
                </Label>
                {pessoaSelecionada ? (
                  <div className="flex items-center gap-2 rounded border px-3 py-2 bg-muted/40">
                    <User className="h-4 w-4 text-muted-foreground" />
                    <span className="flex-1 text-sm font-medium">{pessoaSelecionada.nome}</span>
                    <Button type="button" variant="ghost" size="sm" onClick={limparPessoa} className="h-6 w-6 p-0">
                      <X className="h-3 w-3" />
                    </Button>
                  </div>
                ) : (
                  <div className="relative">
                    <Input
                      value={pessoaBusca}
                      onChange={(e) => setPessoaBusca(e.target.value)}
                      placeholder="Buscar membro pelo nome..."
                    />
                    {buscandoPessoa && (
                      <div className="absolute right-3 top-2.5 text-xs text-muted-foreground">Buscando...</div>
                    )}
                    {resultadosBusca.length > 0 && (
                      <div className="absolute z-10 mt-1 w-full rounded border bg-white shadow-lg">
                        {resultadosBusca.map((p) => (
                          <button
                            key={p.id}
                            type="button"
                            onClick={() => selecionarPessoa(p)}
                            className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm hover:bg-muted"
                          >
                            <User className="h-3 w-3 text-muted-foreground" />
                            {p.nome}
                          </button>
                        ))}
                      </div>
                    )}
                  </div>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="contaBancariaId">{t('finance.revenues.form.fields.bankAccount')}</Label>
                <select id="contaBancariaId" name="contaBancariaId" value={formData.contaBancariaId} onChange={handleChange} className="w-full px-3 py-2 border rounded">
                  <option value="">-</option>
                  {contas.map((c) => (
                    <option key={c.id} value={c.id}>{c.nome}</option>
                  ))}
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="centroCustoId">{t('finance.revenues.form.fields.costCenter')}</Label>
                <select id="centroCustoId" name="centroCustoId" value={formData.centroCustoId} onChange={handleChange} className="w-full px-3 py-2 border rounded">
                  <option value="">-</option>
                  {centrosCustos.map((c) => (
                    <option key={c.id} value={c.id}>{c.nome}</option>
                  ))}
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="projetoId">{t('finance.revenues.form.fields.project')}</Label>
                <select id="projetoId" name="projetoId" value={formData.projetoId} onChange={handleChange} className="w-full px-3 py-2 border rounded">
                  <option value="">-</option>
                  {projetos.map((p) => (
                    <option key={p.id} value={p.id}>{p.nome}</option>
                  ))}
                </select>
              </div>
              {/* Recorrência */}
              <div className="space-y-3 md:col-span-2 rounded border p-3 bg-muted/20">
                <div className="flex items-center gap-3">
                  <input
                    type="checkbox"
                    id="recorrente"
                    checked={formData.recorrente}
                    onChange={(e) => setFormData((prev) => ({ ...prev, recorrente: e.target.checked, tipoRecorrencia: e.target.checked ? prev.tipoRecorrencia || '3' : null }))}
                    className="h-4 w-4 cursor-pointer"
                  />
                  <Label htmlFor="recorrente" className="cursor-pointer">Esta é uma receita recorrente</Label>
                </div>
                {formData.recorrente && (
                  <div className="space-y-1">
                    <Label className="text-sm">Periodicidade *</Label>
                    <select
                      value={formData.tipoRecorrencia || '3'}
                      onChange={(e) => setFormData((prev) => ({ ...prev, tipoRecorrencia: e.target.value }))}
                      className="px-3 py-2 border rounded text-sm"
                    >
                      <option value="1">Semanal</option>
                      <option value="2">Quinzenal</option>
                      <option value="3">Mensal</option>
                      <option value="4">Bimestral</option>
                      <option value="5">Trimestral</option>
                      <option value="6">Semestral</option>
                      <option value="7">Anual</option>
                    </select>
                  </div>
                )}
              </div>

              <div className="space-y-2 md:col-span-2">
                <Label htmlFor="observacoes">{t('finance.revenues.form.fields.notes')}</Label>
                <Textarea id="observacoes" name="observacoes" value={formData.observacoes} onChange={handleChange} placeholder={t('finance.revenues.form.fields.notesPlaceholder')} rows={3} />
              </div>
              <div className="space-y-2 md:col-span-2">
                <Label htmlFor="comprovanteUrl">{t('finance.revenues.form.fields.receiptUrl')}</Label>
                <Input id="comprovanteUrl" name="comprovanteUrl" value={formData.comprovanteUrl} onChange={handleChange} placeholder="https://..." />
              </div>
            </div>
          </CardContent>
        </Card>

        <div className="flex items-center space-x-4">
          <Button type="submit" disabled={loading}>
            <Save className="h-4 w-4 mr-2" /> {loading ? t('actions.saving') : (isEditing ? t('actions.update') : t('actions.create'))}
          </Button>
          <Button type="button" variant="outline" asChild>
            <Link to="/financeiro/receitas">{t('actions.cancel')}</Link>
          </Button>
        </div>
      </form>
    </div>
  );
}
