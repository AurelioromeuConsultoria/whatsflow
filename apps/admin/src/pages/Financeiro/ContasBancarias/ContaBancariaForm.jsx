import { useEffect, useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { ArrowLeft, Save } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { contasBancariasApi } from '@/lib/api';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

export default function ContaBancariaForm() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);
  const { t } = useTranslation();

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const [formData, setFormData] = useState({
    nome: '',
    banco: '',
    agencia: '',
    conta: '',
    tipoConta: '',
    saldoInicial: '',
    ativo: true,
  });

  const load = async () => {
    if (!isEditing) return;
    try {
      setLoading(true);
      setError(null);
      const res = await contasBancariasApi.getById(id);
      const c = res.data || {};
      setFormData({
        nome: c.nome || '',
        banco: c.banco || '',
        agencia: c.agencia || '',
        conta: c.conta || '',
        tipoConta: c.tipoConta || '',
        saldoInicial: c.saldoInicial !== undefined ? String(c.saldoInicial) : '',
        ativo: c.ativo !== undefined ? c.ativo : true,
      });
    } catch (err) {
      setError(t('finance.bankAccounts.saveError'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [id]);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({ ...prev, [name]: type === 'checkbox' ? checked : value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.nome.trim()) {
      toast.error(t('finance.bankAccounts.nameRequired'));
      return;
    }
    if (!formData.saldoInicial) {
      toast.error(t('finance.bankAccounts.initialBalanceRequired'));
      return;
    }

    try {
      setLoading(true);
      const payload = {
        nome: formData.nome.trim(),
        banco: formData.banco?.trim() || null,
        agencia: formData.agencia?.trim() || null,
        conta: formData.conta?.trim() || null,
        tipoConta: formData.tipoConta?.trim() || null,
        saldoInicial: parseFloat(formData.saldoInicial) || 0,
        ativo: formData.ativo,
      };
      if (isEditing) await contasBancariasApi.update(id, payload);
      else await contasBancariasApi.create(payload);
      toast.success(isEditing ? t('finance.bankAccounts.saveSuccessEdit') : t('finance.bankAccounts.saveSuccessCreate'));
      navigate('/financeiro/contas-bancarias');
    } catch (err) {
      const errorMessage = err.response?.data?.message || t('finance.bankAccounts.saveError');
      toast.error(errorMessage);
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading && isEditing) return <LoadingPage text={t('finance.bankAccounts.loadingForm')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to="/financeiro/contas-bancarias">
            <ArrowLeft className="h-4 w-4 mr-2" /> {t('actions.back')}
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">{isEditing ? t('finance.bankAccounts.editTitle') : t('finance.bankAccounts.newTitle')}</h1>
          <p className="text-muted-foreground">{isEditing ? t('finance.bankAccounts.editSubtitle') : t('finance.bankAccounts.newSubtitle')}</p>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>{t('finance.bankAccounts.cardTitle')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="nome">{t('finance.common.name')} *</Label>
                <Input id="nome" name="nome" value={formData.nome} onChange={handleChange} placeholder={t('finance.common.name')} required />
              </div>
              <div className="space-y-2">
                <Label htmlFor="banco">{t('finance.bankAccounts.fieldBank')}</Label>
                <Input id="banco" name="banco" value={formData.banco} onChange={handleChange} placeholder={t('finance.bankAccounts.fieldBank')} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="agencia">{t('finance.bankAccounts.fieldAgency')}</Label>
                <Input id="agencia" name="agencia" value={formData.agencia} onChange={handleChange} placeholder={t('finance.bankAccounts.fieldAgency')} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="conta">{t('finance.bankAccounts.fieldAccount')}</Label>
                <Input id="conta" name="conta" value={formData.conta} onChange={handleChange} placeholder={t('finance.bankAccounts.fieldAccount')} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="tipoConta">{t('finance.bankAccounts.tableType')}</Label>
                <select id="tipoConta" name="tipoConta" value={formData.tipoConta} onChange={handleChange} className="w-full px-3 py-2 border rounded">
                  <option value="">{t('actions.select')}</option>
                  <option value="Corrente">{t('finance.bankAccounts.typeCurrent')}</option>
                  <option value="Poupança">{t('finance.bankAccounts.typeSavings')}</option>
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="saldoInicial">{t('finance.bankAccounts.tableInitialBalance')} *</Label>
                <Input id="saldoInicial" name="saldoInicial" type="number" step="0.01" value={formData.saldoInicial} onChange={handleChange} placeholder="0.00" required />
              </div>
              <div className="space-y-2 flex items-center space-x-3">
                <input
                  type="checkbox"
                  id="ativo"
                  name="ativo"
                  checked={formData.ativo}
                  onChange={handleChange}
                  className="h-4 w-4"
                />
                <Label htmlFor="ativo" className="cursor-pointer">{t('finance.bankAccounts.activeLabel')}</Label>
              </div>
            </div>
          </CardContent>
        </Card>

        <div className="flex items-center space-x-4">
          <Button type="submit" disabled={loading}>
            <Save className="h-4 w-4 mr-2" /> {loading ? t('actions.saving') : (isEditing ? t('actions.update') : t('actions.create'))}
          </Button>
          <Button type="button" variant="outline" asChild>
            <Link to="/financeiro/contas-bancarias">{t('actions.cancel')}</Link>
          </Button>
        </div>
      </form>
    </div>
  );
}
