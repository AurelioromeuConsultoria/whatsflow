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
import { patrimonioApi, categoriasPatrimonioApi, fornecedoresApi, centrosCustosApi, projetosApi, despesasApi, pessoasApi } from '@/lib/api';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

const initialFormData = {
  codigo: '',
  nome: '',
  descricao: '',
  categoriaPatrimonioId: '',
  marca: '',
  modelo: '',
  numeroSerie: '',
  quantidade: '1',
  campus: '',
  localizacao: '',
  ministerioArea: '',
  responsavelPessoaId: '',
  tipoAquisicao: 'Comprado',
  dataAquisicao: '',
  valorAquisicao: '',
  fornecedorId: '',
  numeroNotaFiscal: '',
  despesaId: '',
  centroCustoId: '',
  projetoId: '',
  status: 'EmUso',
  estadoConservacao: 'Bom',
  dataUltimaAvaliacao: '',
  possuiGarantia: false,
  garantiaAte: '',
  dataUltimaManutencao: '',
  dataProximaManutencao: '',
  fotoUrl: '',
  documentoUrl: '',
  observacoes: '',
  ativo: true,
};

export default function PatrimonioForm() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);
  const { t } = useTranslation();

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [categorias, setCategorias] = useState([]);
  const [fornecedores, setFornecedores] = useState([]);
  const [centrosCustos, setCentrosCustos] = useState([]);
  const [projetos, setProjetos] = useState([]);
  const [despesas, setDespesas] = useState([]);
  const [pessoas, setPessoas] = useState([]);
  const [formData, setFormData] = useState(initialFormData);

  const toInputDate = (value) => (value ? new Date(value).toISOString().slice(0, 10) : '');

  const loadDependencies = async () => {
    const [categoriasRes, fornecedoresRes, centrosRes, projetosRes, despesasRes, pessoasRes] = await Promise.all([
      categoriasPatrimonioApi.getAll(),
      fornecedoresApi.getAll(),
      centrosCustosApi.getAll(),
      projetosApi.getAll(),
      despesasApi.getAll(),
      pessoasApi.getAll(),
    ]);

    setCategorias(categoriasRes.data || []);
    setFornecedores(fornecedoresRes.data || []);
    setCentrosCustos(centrosRes.data || []);
    setProjetos(projetosRes.data || []);
    setDespesas(despesasRes.data || []);
    setPessoas((pessoasRes.data || []).filter((pessoa) => pessoa.ativo));
  };

  const load = async () => {
    try {
      setLoading(true);
      setError(null);
      await loadDependencies();

      if (!isEditing) return;

      const res = await patrimonioApi.getById(id);
      const item = res.data || {};
      setFormData({
        codigo: item.codigo || '',
        nome: item.nome || '',
        descricao: item.descricao || '',
        categoriaPatrimonioId: item.categoriaPatrimonioId ? String(item.categoriaPatrimonioId) : '',
        marca: item.marca || '',
        modelo: item.modelo || '',
        numeroSerie: item.numeroSerie || '',
        quantidade: item.quantidade ? String(item.quantidade) : '1',
        campus: item.campus || '',
        localizacao: item.localizacao || '',
        ministerioArea: item.ministerioArea || '',
        responsavelPessoaId: item.responsavelPessoaId ? String(item.responsavelPessoaId) : '',
        tipoAquisicao: item.tipoAquisicao || 'Comprado',
        dataAquisicao: toInputDate(item.dataAquisicao),
        valorAquisicao: item.valorAquisicao !== undefined && item.valorAquisicao !== null ? String(item.valorAquisicao) : '',
        fornecedorId: item.fornecedorId ? String(item.fornecedorId) : '',
        numeroNotaFiscal: item.numeroNotaFiscal || '',
        despesaId: item.despesaId ? String(item.despesaId) : '',
        centroCustoId: item.centroCustoId ? String(item.centroCustoId) : '',
        projetoId: item.projetoId ? String(item.projetoId) : '',
        status: item.status || 'EmUso',
        estadoConservacao: item.estadoConservacao || 'Bom',
        dataUltimaAvaliacao: toInputDate(item.dataUltimaAvaliacao),
        possuiGarantia: Boolean(item.possuiGarantia),
        garantiaAte: toInputDate(item.garantiaAte),
        dataUltimaManutencao: toInputDate(item.dataUltimaManutencao),
        dataProximaManutencao: toInputDate(item.dataProximaManutencao),
        fotoUrl: item.fotoUrl || '',
        documentoUrl: item.documentoUrl || '',
        observacoes: item.observacoes || '',
        ativo: item.ativo !== undefined ? item.ativo : true,
      });
    } catch (err) {
      setError(t('finance.patrimony.saveError'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [id]);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({ ...prev, [name]: type === 'checkbox' ? checked : value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.codigo.trim()) {
      toast.error(t('finance.patrimony.validation.codeRequired'));
      return;
    }
    if (!formData.nome.trim()) {
      toast.error(t('finance.patrimony.validation.nameRequired'));
      return;
    }
    if (!formData.categoriaPatrimonioId) {
      toast.error(t('finance.patrimony.validation.categoryRequired'));
      return;
    }

    try {
      setLoading(true);
      const payload = {
        codigo: formData.codigo.trim(),
        nome: formData.nome.trim(),
        descricao: formData.descricao?.trim() || null,
        categoriaPatrimonioId: Number(formData.categoriaPatrimonioId),
        marca: formData.marca?.trim() || null,
        modelo: formData.modelo?.trim() || null,
        numeroSerie: formData.numeroSerie?.trim() || null,
        quantidade: Number(formData.quantidade) || 1,
        campus: formData.campus?.trim() || null,
        localizacao: formData.localizacao?.trim() || null,
        ministerioArea: formData.ministerioArea?.trim() || null,
        responsavelPessoaId: formData.responsavelPessoaId ? Number(formData.responsavelPessoaId) : null,
        tipoAquisicao: formData.tipoAquisicao,
        dataAquisicao: formData.dataAquisicao ? new Date(formData.dataAquisicao).toISOString() : null,
        valorAquisicao: formData.valorAquisicao ? parseFloat(formData.valorAquisicao) : null,
        fornecedorId: formData.fornecedorId ? Number(formData.fornecedorId) : null,
        numeroNotaFiscal: formData.numeroNotaFiscal?.trim() || null,
        despesaId: formData.despesaId ? Number(formData.despesaId) : null,
        centroCustoId: formData.centroCustoId ? Number(formData.centroCustoId) : null,
        projetoId: formData.projetoId ? Number(formData.projetoId) : null,
        status: formData.status,
        estadoConservacao: formData.estadoConservacao,
        dataUltimaAvaliacao: formData.dataUltimaAvaliacao ? new Date(formData.dataUltimaAvaliacao).toISOString() : null,
        possuiGarantia: formData.possuiGarantia,
        garantiaAte: formData.garantiaAte ? new Date(formData.garantiaAte).toISOString() : null,
        dataUltimaManutencao: formData.dataUltimaManutencao ? new Date(formData.dataUltimaManutencao).toISOString() : null,
        dataProximaManutencao: formData.dataProximaManutencao ? new Date(formData.dataProximaManutencao).toISOString() : null,
        fotoUrl: formData.fotoUrl?.trim() || null,
        documentoUrl: formData.documentoUrl?.trim() || null,
        observacoes: formData.observacoes?.trim() || null,
        ativo: formData.ativo,
      };

      if (isEditing) await patrimonioApi.update(id, payload);
      else await patrimonioApi.create(payload);

      toast.success(isEditing ? t('finance.patrimony.saveSuccessEdit') : t('finance.patrimony.saveSuccessCreate'));
      navigate('/financeiro/patrimonio');
    } catch (err) {
      toast.error(err.response?.data?.message || t('finance.patrimony.saveError'));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading && isEditing) return <LoadingPage text={t('finance.patrimony.loadingForm')} />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to="/financeiro/patrimonio">
            <ArrowLeft className="h-4 w-4 mr-2" /> {t('actions.back')}
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">{isEditing ? t('finance.patrimony.editTitle') : t('finance.patrimony.newTitle')}</h1>
          <p className="text-muted-foreground">{isEditing ? t('finance.patrimony.editSubtitle') : t('finance.patrimony.newSubtitle')}</p>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        <Card>
          <CardHeader><CardTitle>{t('finance.patrimony.sections.identification')}</CardTitle></CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-3">
              <div className="space-y-2">
                <Label htmlFor="codigo">{t('finance.patrimony.fields.code')} *</Label>
                <Input id="codigo" name="codigo" value={formData.codigo} onChange={handleChange} placeholder="PAT-001" required />
              </div>
              <div className="space-y-2 md:col-span-2">
                <Label htmlFor="nome">{t('finance.patrimony.fields.name')} *</Label>
                <Input id="nome" name="nome" value={formData.nome} onChange={handleChange} required />
              </div>
              <div className="space-y-2">
                <Label htmlFor="categoriaPatrimonioId">{t('finance.patrimony.fields.category')} *</Label>
                <select id="categoriaPatrimonioId" name="categoriaPatrimonioId" value={formData.categoriaPatrimonioId} onChange={handleChange} className="w-full px-3 py-2 border rounded" required>
                  <option value="">{t('actions.select')}</option>
                  {categorias.map((categoria) => <option key={categoria.id} value={categoria.id}>{categoria.nome}</option>)}
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="quantidade">{t('finance.patrimony.fields.quantity')}</Label>
                <Input id="quantidade" name="quantidade" type="number" min="1" value={formData.quantidade} onChange={handleChange} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="numeroSerie">{t('finance.patrimony.fields.serialNumber')}</Label>
                <Input id="numeroSerie" name="numeroSerie" value={formData.numeroSerie} onChange={handleChange} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="marca">{t('finance.patrimony.fields.brand')}</Label>
                <Input id="marca" name="marca" value={formData.marca} onChange={handleChange} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="modelo">{t('finance.patrimony.fields.model')}</Label>
                <Input id="modelo" name="modelo" value={formData.modelo} onChange={handleChange} />
              </div>
              <div className="space-y-2 md:col-span-3">
                <Label htmlFor="descricao">{t('finance.common.description')}</Label>
                <Textarea id="descricao" name="descricao" value={formData.descricao} onChange={handleChange} rows={3} />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle>{t('finance.patrimony.sections.location')}</CardTitle></CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="campus">{t('finance.patrimony.fields.campus')}</Label>
                <Input id="campus" name="campus" value={formData.campus} onChange={handleChange} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="localizacao">{t('finance.patrimony.fields.location')}</Label>
                <Input id="localizacao" name="localizacao" value={formData.localizacao} onChange={handleChange} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="ministerioArea">{t('finance.patrimony.fields.ministry')}</Label>
                <Input id="ministerioArea" name="ministerioArea" value={formData.ministerioArea} onChange={handleChange} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="responsavelPessoaId">{t('finance.patrimony.fields.responsible')}</Label>
                <select id="responsavelPessoaId" name="responsavelPessoaId" value={formData.responsavelPessoaId} onChange={handleChange} className="w-full px-3 py-2 border rounded">
                  <option value="">{t('actions.select')}</option>
                  {pessoas.map((pessoa) => <option key={pessoa.id} value={pessoa.id}>{pessoa.nome}</option>)}
                </select>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle>{t('finance.patrimony.sections.acquisition')}</CardTitle></CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-3">
              <div className="space-y-2">
                <Label htmlFor="tipoAquisicao">{t('finance.patrimony.fields.acquisitionType')}</Label>
                <select id="tipoAquisicao" name="tipoAquisicao" value={formData.tipoAquisicao} onChange={handleChange} className="w-full px-3 py-2 border rounded">
                  <option value="Comprado">{t('finance.patrimony.acquisitionType.purchased')}</option>
                  <option value="Doado">{t('finance.patrimony.acquisitionType.donated')}</option>
                  <option value="Fabricado">{t('finance.patrimony.acquisitionType.manufactured')}</option>
                  <option value="Cedido">{t('finance.patrimony.acquisitionType.assigned')}</option>
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="dataAquisicao">{t('finance.patrimony.fields.acquisitionDate')}</Label>
                <Input id="dataAquisicao" name="dataAquisicao" type="date" value={formData.dataAquisicao} onChange={handleChange} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="valorAquisicao">{t('finance.patrimony.fields.acquisitionValue')}</Label>
                <Input id="valorAquisicao" name="valorAquisicao" type="number" step="0.01" value={formData.valorAquisicao} onChange={handleChange} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="fornecedorId">{t('finance.patrimony.fields.supplier')}</Label>
                <select id="fornecedorId" name="fornecedorId" value={formData.fornecedorId} onChange={handleChange} className="w-full px-3 py-2 border rounded">
                  <option value="">{t('actions.select')}</option>
                  {fornecedores.map((fornecedor) => <option key={fornecedor.id} value={fornecedor.id}>{fornecedor.nome}</option>)}
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="numeroNotaFiscal">{t('finance.patrimony.fields.invoiceNumber')}</Label>
                <Input id="numeroNotaFiscal" name="numeroNotaFiscal" value={formData.numeroNotaFiscal} onChange={handleChange} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="despesaId">{t('finance.patrimony.fields.expense')}</Label>
                <select id="despesaId" name="despesaId" value={formData.despesaId} onChange={handleChange} className="w-full px-3 py-2 border rounded">
                  <option value="">{t('actions.select')}</option>
                  {despesas.map((despesa) => <option key={despesa.id} value={despesa.id}>{despesa.descricao}</option>)}
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="centroCustoId">{t('finance.patrimony.fields.costCenter')}</Label>
                <select id="centroCustoId" name="centroCustoId" value={formData.centroCustoId} onChange={handleChange} className="w-full px-3 py-2 border rounded">
                  <option value="">{t('actions.select')}</option>
                  {centrosCustos.map((centro) => <option key={centro.id} value={centro.id}>{centro.nome}</option>)}
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="projetoId">{t('finance.patrimony.fields.project')}</Label>
                <select id="projetoId" name="projetoId" value={formData.projetoId} onChange={handleChange} className="w-full px-3 py-2 border rounded">
                  <option value="">{t('actions.select')}</option>
                  {projetos.map((projeto) => <option key={projeto.id} value={projeto.id}>{projeto.nome}</option>)}
                </select>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle>{t('finance.patrimony.sections.state')}</CardTitle></CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-3">
              <div className="space-y-2">
                <Label htmlFor="status">{t('finance.patrimony.fields.status')}</Label>
                <select id="status" name="status" value={formData.status} onChange={handleChange} className="w-full px-3 py-2 border rounded">
                  <option value="EmUso">{t('finance.patrimony.status.inUse')}</option>
                  <option value="EmManutencao">{t('finance.patrimony.status.inMaintenance')}</option>
                  <option value="Emprestado">{t('finance.patrimony.status.loaned')}</option>
                  <option value="Ocioso">{t('finance.patrimony.status.idle')}</option>
                  <option value="Baixado">{t('finance.patrimony.status.disposed')}</option>
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="estadoConservacao">{t('finance.patrimony.fields.conservationState')}</Label>
                <select id="estadoConservacao" name="estadoConservacao" value={formData.estadoConservacao} onChange={handleChange} className="w-full px-3 py-2 border rounded">
                  <option value="Novo">{t('finance.patrimony.conservationState.new')}</option>
                  <option value="Bom">{t('finance.patrimony.conservationState.good')}</option>
                  <option value="Regular">{t('finance.patrimony.conservationState.fair')}</option>
                  <option value="Ruim">{t('finance.patrimony.conservationState.poor')}</option>
                  <option value="Inutilizavel">{t('finance.patrimony.conservationState.unusable')}</option>
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="dataUltimaAvaliacao">{t('finance.patrimony.fields.lastEvaluation')}</Label>
                <Input id="dataUltimaAvaliacao" name="dataUltimaAvaliacao" type="date" value={formData.dataUltimaAvaliacao} onChange={handleChange} />
              </div>
              <div className="space-y-2 flex items-center space-x-3">
                <input type="checkbox" id="possuiGarantia" name="possuiGarantia" checked={formData.possuiGarantia} onChange={handleChange} className="h-4 w-4" />
                <Label htmlFor="possuiGarantia" className="cursor-pointer">{t('finance.patrimony.fields.hasWarranty')}</Label>
              </div>
              <div className="space-y-2">
                <Label htmlFor="garantiaAte">{t('finance.patrimony.fields.warrantyUntil')}</Label>
                <Input id="garantiaAte" name="garantiaAte" type="date" value={formData.garantiaAte} onChange={handleChange} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="dataUltimaManutencao">{t('finance.patrimony.fields.lastMaintenance')}</Label>
                <Input id="dataUltimaManutencao" name="dataUltimaManutencao" type="date" value={formData.dataUltimaManutencao} onChange={handleChange} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="dataProximaManutencao">{t('finance.patrimony.fields.nextMaintenance')}</Label>
                <Input id="dataProximaManutencao" name="dataProximaManutencao" type="date" value={formData.dataProximaManutencao} onChange={handleChange} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="fotoUrl">{t('finance.patrimony.fields.photoUrl')}</Label>
                <Input id="fotoUrl" name="fotoUrl" value={formData.fotoUrl} onChange={handleChange} placeholder="https://..." />
              </div>
              <div className="space-y-2">
                <Label htmlFor="documentoUrl">{t('finance.patrimony.fields.documentUrl')}</Label>
                <Input id="documentoUrl" name="documentoUrl" value={formData.documentoUrl} onChange={handleChange} placeholder="https://..." />
              </div>
              <div className="space-y-2 md:col-span-3">
                <Label htmlFor="observacoes">{t('finance.patrimony.fields.notes')}</Label>
                <Textarea id="observacoes" name="observacoes" value={formData.observacoes} onChange={handleChange} rows={4} />
              </div>
              <div className="space-y-2 flex items-center space-x-3">
                <input type="checkbox" id="ativo" name="ativo" checked={formData.ativo} onChange={handleChange} className="h-4 w-4" />
                <Label htmlFor="ativo" className="cursor-pointer">{t('finance.patrimony.fields.active')}</Label>
              </div>
            </div>
          </CardContent>
        </Card>

        <div className="flex items-center space-x-4">
          <Button type="submit" disabled={loading}>
            <Save className="h-4 w-4 mr-2" /> {loading ? t('actions.saving') : (isEditing ? t('actions.update') : t('actions.create'))}
          </Button>
          <Button type="button" variant="outline" asChild>
            <Link to="/financeiro/patrimonio">{t('actions.cancel')}</Link>
          </Button>
        </div>
      </form>
    </div>
  );
}
