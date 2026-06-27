import { useEffect, useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { ArrowLeft, Save } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { despesasApi, fornecedoresApi, categoriasDespesasApi, contasBancariasApi, centrosCustosApi, projetosApi } from '@/lib/api';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

export default function DespesaForm() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);
  const { t } = useTranslation();

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [fornecedores, setFornecedores] = useState([]);
  const [categorias, setCategorias] = useState([]);
  const [contas, setContas] = useState([]);
  const [centrosCustos, setCentrosCustos] = useState([]);
  const [projetos, setProjetos] = useState([]);

  const [formData, setFormData] = useState({
    descricao: '',
    valor: '',
    dataVencimento: '',
    status: 'Pendente',
    observacoes: '',
    comprovanteUrl: '',
    fornecedorId: '',
    categoriaDespesaId: '',
    contaBancariaId: '',
    centroCustoId: '',
    projetoId: '',
    recorrente: false,
    tipoRecorrencia: null,
  });

  const loadDependencies = async () => {
    try {
      const [fornecedoresRes, categoriasRes, contasRes, centrosRes, projetosRes] = await Promise.all([
        fornecedoresApi.getAll(),
        categoriasDespesasApi.getAll(),
        contasBancariasApi.getAll(),
        centrosCustosApi.getAll(),
        projetosApi.getAll(),
      ]);
      setFornecedores(fornecedoresRes.data || []);
      setCategorias(categoriasRes.data || []);
      setContas(contasRes.data || []);
      setCentrosCustos(centrosRes.data || []);
      setProjetos(projetosRes.data || []);
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
      const res = await despesasApi.getById(id);
      const d = res.data || {};
      setFormData({
        descricao: d.descricao || '',
        valor: d.valor !== undefined ? String(d.valor) : '',
        dataVencimento: d.dataVencimento ? new Date(d.dataVencimento).toISOString().slice(0, 10) : '',
        status: d.status || 'Pendente',
        observacoes: d.observacoes || '',
        comprovanteUrl: d.comprovanteUrl || '',
        fornecedorId: d.fornecedorId ? String(d.fornecedorId) : '',
        categoriaDespesaId: d.categoriaDespesaId ? String(d.categoriaDespesaId) : '',
        contaBancariaId: d.contaBancariaId ? String(d.contaBancariaId) : '',
        centroCustoId: d.centroCustoId ? String(d.centroCustoId) : '',
        projetoId: d.projetoId ? String(d.projetoId) : '',
        recorrente: d.recorrente || false,
        tipoRecorrencia: d.tipoRecorrencia || null,
      });
    } catch (err) {
      setError(t('finance.expenses.form.saveError'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [id]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.descricao.trim()) {
      toast.error(t('finance.expenses.form.validation.descriptionRequired'));
      return;
    }
    if (!formData.valor) {
      toast.error(t('finance.expenses.form.validation.valueRequired'));
      return;
    }
    if (!formData.dataVencimento) {
      toast.error(t('finance.expenses.form.validation.dueDateRequired'));
      return;
    }

    try {
      setLoading(true);
      const payload = {
        descricao: formData.descricao.trim(),
        valor: parseFloat(formData.valor) || 0,
        dataVencimento: new Date(formData.dataVencimento).toISOString(),
        status: formData.status,
        observacoes: formData.observacoes?.trim() || null,
        comprovanteUrl: formData.comprovanteUrl?.trim() || null,
        fornecedorId: formData.fornecedorId ? Number(formData.fornecedorId) : null,
        categoriaDespesaId: formData.categoriaDespesaId ? Number(formData.categoriaDespesaId) : null,
        contaBancariaId: formData.contaBancariaId ? Number(formData.contaBancariaId) : null,
        centroCustoId: formData.centroCustoId ? Number(formData.centroCustoId) : null,
        projetoId: formData.projetoId ? Number(formData.projetoId) : null,
        recorrente: formData.recorrente,
        tipoRecorrencia: formData.recorrente && formData.tipoRecorrencia ? Number(formData.tipoRecorrencia) : null,
      };
      if (isEditing) await despesasApi.update(id, payload);
      else await despesasApi.create(payload);
      toast.success(isEditing ? t('finance.expenses.form.saveSuccessEdit') : t('finance.expenses.form.saveSuccessCreate'));
      navigate('/financeiro/despesas');
    } catch (err) {
      const errorMessage = err.response?.data?.message || t('finance.expenses.form.saveError');
      toast.error(errorMessage);
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading && isEditing) return <LoadingPage text={t('finance.expenses.form.loading')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to="/financeiro/despesas">
            <ArrowLeft className="h-4 w-4 mr-2" /> {t('actions.back')}
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">{isEditing ? t('finance.expenses.form.editTitle') : t('finance.expenses.form.newTitle')}</h1>
          <p className="text-muted-foreground">{isEditing ? t('finance.expenses.form.editSubtitle') : t('finance.expenses.form.newSubtitle')}</p>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>{t('finance.expenses.form.cardTitle')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2 md:col-span-2">
                <Label htmlFor="descricao">{t('finance.expenses.form.fields.description')} *</Label>
                <Input id="descricao" name="descricao" value={formData.descricao} onChange={handleChange} placeholder={t('finance.expenses.form.fields.descriptionPlaceholder')} required />
              </div>
              <div className="space-y-2">
                <Label htmlFor="valor">{t('finance.expenses.form.fields.value')} *</Label>
                <Input id="valor" name="valor" type="number" step="0.01" value={formData.valor} onChange={handleChange} placeholder="0.00" required />
              </div>
              <div className="space-y-2">
                <Label htmlFor="dataVencimento">{t('finance.expenses.form.fields.dueDate')} *</Label>
                <Input id="dataVencimento" name="dataVencimento" type="date" value={formData.dataVencimento} onChange={handleChange} required />
              </div>
              <div className="space-y-2">
                <Label htmlFor="status">{t('finance.expenses.form.fields.status')} *</Label>
                <select id="status" name="status" value={formData.status} onChange={handleChange} className="w-full px-3 py-2 border rounded" required>
                  <option value="Pendente">{t('finance.expenses.status.pending')}</option>
                  <option value="Pago">{t('finance.expenses.status.paid')}</option>
                  <option value="Cancelado">{t('finance.expenses.status.canceled')}</option>
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="fornecedorId">{t('finance.expenses.form.fields.supplier')}</Label>
                <select id="fornecedorId" name="fornecedorId" value={formData.fornecedorId} onChange={handleChange} className="w-full px-3 py-2 border rounded">
                  <option value="">{t('actions.select')}</option>
                  {fornecedores.map((f) => (
                    <option key={f.id} value={f.id}>{f.nome}</option>
                  ))}
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="categoriaDespesaId">{t('finance.expenses.form.fields.category')}</Label>
                <select id="categoriaDespesaId" name="categoriaDespesaId" value={formData.categoriaDespesaId} onChange={handleChange} className="w-full px-3 py-2 border rounded">
                  <option value="">{t('actions.select')}</option>
                  {categorias.map((c) => (
                    <option key={c.id} value={c.id}>{c.nome}</option>
                  ))}
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="contaBancariaId">{t('finance.expenses.form.fields.bankAccount')}</Label>
                <select id="contaBancariaId" name="contaBancariaId" value={formData.contaBancariaId} onChange={handleChange} className="w-full px-3 py-2 border rounded">
                  <option value="">{t('actions.select')}</option>
                  {contas.map((c) => (
                    <option key={c.id} value={c.id}>{c.nome}</option>
                  ))}
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="centroCustoId">{t('finance.expenses.form.fields.costCenter')}</Label>
                <select id="centroCustoId" name="centroCustoId" value={formData.centroCustoId} onChange={handleChange} className="w-full px-3 py-2 border rounded">
                  <option value="">{t('actions.select')}</option>
                  {centrosCustos.map((c) => (
                    <option key={c.id} value={c.id}>{c.nome}</option>
                  ))}
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="projetoId">{t('finance.expenses.form.fields.project')}</Label>
                <select id="projetoId" name="projetoId" value={formData.projetoId} onChange={handleChange} className="w-full px-3 py-2 border rounded">
                  <option value="">{t('actions.select')}</option>
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
                  <Label htmlFor="recorrente" className="cursor-pointer">Esta é uma despesa recorrente</Label>
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
                <Label htmlFor="observacoes">{t('finance.expenses.form.fields.notes')}</Label>
                <Textarea id="observacoes" name="observacoes" value={formData.observacoes} onChange={handleChange} placeholder={t('finance.expenses.form.fields.notesPlaceholder')} rows={3} />
              </div>
              <div className="space-y-2 md:col-span-2">
                <Label htmlFor="comprovanteUrl">{t('finance.expenses.form.fields.receiptUrl')}</Label>
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
            <Link to="/financeiro/despesas">{t('actions.cancel')}</Link>
          </Button>
        </div>
      </form>
    </div>
  );
}
