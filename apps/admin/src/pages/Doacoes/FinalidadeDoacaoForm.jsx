import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Save } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { categoriasReceitasApi, centrosCustosApi, contasBancariasApi, finalidadesDoacaoApi, projetosApi } from '@/lib/api';

const EMPTY_VALUE = '__none__';

function parseValores(value) {
  return String(value || '')
    .split(',')
    .map((item) => Number(String(item).trim().replace(',', '.')))
    .filter((item) => Number.isFinite(item) && item > 0);
}

function toSelectValue(value) {
  return value ? String(value) : EMPTY_VALUE;
}

function fromSelectValue(value) {
  return value === EMPTY_VALUE ? null : Number(value);
}

export default function FinalidadeDoacaoForm() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);
  const [loading, setLoading] = useState(isEditing);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  const [options, setOptions] = useState({
    categorias: [],
    contas: [],
    centros: [],
    projetos: [],
  });
  const [formData, setFormData] = useState({
    nome: '',
    slug: '',
    descricaoPublica: '',
    imagemUrl: '',
    corHex: '',
    valoresSugeridos: '20, 50, 100, 200',
    valorMinimo: '1',
    ordem: '0',
    ativo: true,
    visivelPortal: true,
    permiteAnonimo: true,
    permitePix: true,
    permiteCartaoCredito: false,
    categoriaReceitaId: null,
    contaBancariaId: null,
    centroCustoId: null,
    projetoId: null,
  });

  const loadOptions = async () => {
    const [categorias, contas, centros, projetos] = await Promise.all([
      categoriasReceitasApi.getAll(),
      contasBancariasApi.getAll(),
      centrosCustosApi.getAll(),
      projetosApi.getAll(),
    ]);

    setOptions({
      categorias: categorias.data || [],
      contas: contas.data || [],
      centros: centros.data || [],
      projetos: projetos.data || [],
    });
  };

  const load = async () => {
    try {
      setLoading(true);
      setError(null);
      await loadOptions();

      if (!isEditing) return;

      const res = await finalidadesDoacaoApi.getById(id);
      const item = res.data || {};
      setFormData({
        nome: item.nome || '',
        slug: item.slug || '',
        descricaoPublica: item.descricaoPublica || '',
        imagemUrl: item.imagemUrl || '',
        corHex: item.corHex || '',
        valoresSugeridos: (item.valoresSugeridos || []).join(', '),
        valorMinimo: item.valorMinimo ? String(item.valorMinimo) : '',
        ordem: String(item.ordem ?? 0),
        ativo: item.ativo ?? true,
        visivelPortal: item.visivelPortal ?? true,
        permiteAnonimo: item.permiteAnonimo ?? true,
        permitePix: item.permitePix ?? true,
        permiteCartaoCredito: item.permiteCartaoCredito ?? false,
        categoriaReceitaId: item.categoriaReceitaId || null,
        contaBancariaId: item.contaBancariaId || null,
        centroCustoId: item.centroCustoId || null,
        projetoId: item.projetoId || null,
      });
    } catch (err) {
      setError('Não foi possível carregar a finalidade de doação.');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [id]);

  const title = useMemo(() => (isEditing ? 'Editar finalidade' : 'Nova finalidade'), [isEditing]);

  const handleChange = (event) => {
    const { name, value, type, checked } = event.target;
    setFormData((current) => ({ ...current, [name]: type === 'checkbox' ? checked : value }));
  };

  const handleSelect = (name, value) => {
    setFormData((current) => ({ ...current, [name]: fromSelectValue(value) }));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    if (!formData.nome.trim()) {
      toast.error('Informe o nome da finalidade.');
      return;
    }

    if (!formData.permitePix && !formData.permiteCartaoCredito) {
      toast.error('Habilite pelo menos uma forma de pagamento.');
      return;
    }

    const payload = {
      nome: formData.nome.trim(),
      slug: formData.slug.trim() || null,
      descricaoPublica: formData.descricaoPublica.trim() || null,
      imagemUrl: formData.imagemUrl.trim() || null,
      corHex: formData.corHex.trim() || null,
      valoresSugeridos: parseValores(formData.valoresSugeridos),
      valorMinimo: formData.valorMinimo ? Number(formData.valorMinimo) : null,
      ordem: Number(formData.ordem || 0),
      ativo: formData.ativo,
      visivelPortal: formData.visivelPortal,
      permiteAnonimo: formData.permiteAnonimo,
      permitePix: formData.permitePix,
      permiteCartaoCredito: formData.permiteCartaoCredito,
      categoriaReceitaId: formData.categoriaReceitaId,
      contaBancariaId: formData.contaBancariaId,
      centroCustoId: formData.centroCustoId,
      projetoId: formData.projetoId,
    };

    try {
      setSaving(true);
      if (isEditing) await finalidadesDoacaoApi.update(id, payload);
      else await finalidadesDoacaoApi.create(payload);
      toast.success(isEditing ? 'Finalidade atualizada.' : 'Finalidade criada.');
      navigate('/doacoes/finalidades');
    } catch (err) {
      toast.error(err.response?.data?.message || 'Não foi possível salvar a finalidade.');
      console.error(err);
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <LoadingPage text="Carregando finalidade..." />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" asChild>
          <Link to="/doacoes/finalidades">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Voltar
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">{title}</h1>
          <p className="text-muted-foreground">Configure como essa opção aparecerá no Portal e no financeiro.</p>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>Informações públicas</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="nome">Nome *</Label>
              <Input id="nome" name="nome" value={formData.nome} onChange={handleChange} placeholder="Dízimos, Ofertas, Missões..." required />
            </div>
            <div className="space-y-2">
              <Label htmlFor="slug">Slug público</Label>
              <Input id="slug" name="slug" value={formData.slug} onChange={handleChange} placeholder="dizimos" />
            </div>
            <div className="space-y-2 md:col-span-2">
              <Label htmlFor="descricaoPublica">Descrição pública</Label>
              <Textarea id="descricaoPublica" name="descricaoPublica" value={formData.descricaoPublica} onChange={handleChange} rows={3} placeholder="Texto curto exibido no card da página de doações." />
            </div>
            <div className="space-y-2">
              <Label htmlFor="imagemUrl">Imagem URL</Label>
              <Input id="imagemUrl" name="imagemUrl" value={formData.imagemUrl} onChange={handleChange} placeholder="https://..." />
            </div>
            <div className="space-y-2">
              <Label htmlFor="corHex">Cor</Label>
              <Input id="corHex" name="corHex" value={formData.corHex} onChange={handleChange} placeholder="#994B22" />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Valores e pagamento</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="valoresSugeridos">Valores sugeridos</Label>
              <Input id="valoresSugeridos" name="valoresSugeridos" value={formData.valoresSugeridos} onChange={handleChange} placeholder="20, 50, 100, 200" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="valorMinimo">Valor mínimo</Label>
              <Input id="valorMinimo" name="valorMinimo" type="number" min="1" step="0.01" value={formData.valorMinimo} onChange={handleChange} />
            </div>
            <div className="flex flex-wrap gap-5 md:col-span-2">
              {[
                ['permitePix', 'Pix'],
                ['permiteCartaoCredito', 'Cartão de crédito'],
                ['permiteAnonimo', 'Permitir doação anônima'],
              ].map(([name, label]) => (
                <label key={name} className="flex items-center gap-2 text-sm font-medium">
                  <input type="checkbox" name={name} checked={formData[name]} onChange={handleChange} className="h-4 w-4" />
                  {label}
                </label>
              ))}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Vínculos financeiros</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label>Categoria de receita</Label>
              <Select value={toSelectValue(formData.categoriaReceitaId)} onValueChange={(value) => handleSelect('categoriaReceitaId', value)}>
                <SelectTrigger><SelectValue placeholder="Selecione" /></SelectTrigger>
                <SelectContent>
                  <SelectItem value={EMPTY_VALUE}>Sem categoria</SelectItem>
                  {options.categorias.map((item) => <SelectItem key={item.id} value={String(item.id)}>{item.nome}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Conta bancária</Label>
              <Select value={toSelectValue(formData.contaBancariaId)} onValueChange={(value) => handleSelect('contaBancariaId', value)}>
                <SelectTrigger><SelectValue placeholder="Selecione" /></SelectTrigger>
                <SelectContent>
                  <SelectItem value={EMPTY_VALUE}>Sem conta</SelectItem>
                  {options.contas.map((item) => <SelectItem key={item.id} value={String(item.id)}>{item.nome}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Centro de custo</Label>
              <Select value={toSelectValue(formData.centroCustoId)} onValueChange={(value) => handleSelect('centroCustoId', value)}>
                <SelectTrigger><SelectValue placeholder="Selecione" /></SelectTrigger>
                <SelectContent>
                  <SelectItem value={EMPTY_VALUE}>Sem centro</SelectItem>
                  {options.centros.map((item) => <SelectItem key={item.id} value={String(item.id)}>{item.nome}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Projeto</Label>
              <Select value={toSelectValue(formData.projetoId)} onValueChange={(value) => handleSelect('projetoId', value)}>
                <SelectTrigger><SelectValue placeholder="Selecione" /></SelectTrigger>
                <SelectContent>
                  <SelectItem value={EMPTY_VALUE}>Sem projeto</SelectItem>
                  {options.projetos.map((item) => <SelectItem key={item.id} value={String(item.id)}>{item.nome}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Publicação</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <Label htmlFor="ordem">Ordem</Label>
              <Input id="ordem" name="ordem" type="number" value={formData.ordem} onChange={handleChange} />
            </div>
            <label className="flex items-center gap-2 pt-8 text-sm font-medium">
              <input type="checkbox" name="ativo" checked={formData.ativo} onChange={handleChange} className="h-4 w-4" />
              Ativa
            </label>
            <label className="flex items-center gap-2 pt-8 text-sm font-medium">
              <input type="checkbox" name="visivelPortal" checked={formData.visivelPortal} onChange={handleChange} className="h-4 w-4" />
              Visível no Portal
            </label>
          </CardContent>
        </Card>

        <div className="flex items-center gap-3">
          <Button type="submit" disabled={saving}>
            <Save className="mr-2 h-4 w-4" />
            {saving ? 'Salvando...' : 'Salvar'}
          </Button>
          <Button type="button" variant="outline" asChild>
            <Link to="/doacoes/finalidades">Cancelar</Link>
          </Button>
        </div>
      </form>
    </div>
  );
}
