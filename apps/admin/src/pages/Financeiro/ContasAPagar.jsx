import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { AlertTriangle, Clock, Calendar, RefreshCw, CheckCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { LoadingPage } from '@/components/ui/loading';
import { despesasApi } from '@/lib/api';
import { toast } from 'sonner';
import { formatCurrency, formatDate } from '@/lib/formatters';

function DespesaRow({ d, onGerarProxima, onMarcarPaga }) {
  const [loading, setLoading] = useState(false);

  const handleProxima = async () => {
    setLoading(true);
    try {
      await onGerarProxima(d.id);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex items-center gap-3 rounded border px-4 py-3 text-sm hover:bg-muted/30">
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2">
          <span className="font-medium truncate">{d.descricao}</span>
          {d.recorrente && (
            <Badge variant="outline" className="text-xs shrink-0">
              <RefreshCw className="h-3 w-3 mr-1" />{d.tipoRecorrenciaDescricao}
            </Badge>
          )}
        </div>
        <div className="flex gap-3 mt-0.5 text-muted-foreground text-xs">
          {d.fornecedorNome && <span>{d.fornecedorNome}</span>}
          {d.categoriaDespesaNome && <span>{d.categoriaDespesaNome}</span>}
          <span>Vence {formatDate(d.dataVencimento)}</span>
        </div>
      </div>
      <div className="font-bold text-right shrink-0">{formatCurrency(d.valor)}</div>
      <div className="flex items-center gap-1 shrink-0">
        <Button variant="ghost" size="sm" asChild className="h-7 text-xs">
          <Link to={`/financeiro/despesas/${d.id}/editar`}>Editar</Link>
        </Button>
        {d.recorrente && (
          <Button variant="outline" size="sm" onClick={handleProxima} disabled={loading} className="h-7 text-xs">
            <RefreshCw className="h-3 w-3 mr-1" /> {loading ? '...' : 'Próxima'}
          </Button>
        )}
      </div>
    </div>
  );
}

function Secao({ titulo, icon: Icon, cor, itens, total, onGerarProxima }) {
  if (itens.length === 0) return null;
  return (
    <Card>
      <CardHeader>
        <CardTitle className={`flex items-center justify-between ${cor}`}>
          <div className="flex items-center gap-2">
            <Icon className="h-5 w-5" />
            {titulo} ({itens.length})
          </div>
          <span className="text-base">{formatCurrency(total)}</span>
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-1.5">
        {itens.map((d) => (
          <DespesaRow key={d.id} d={d} onGerarProxima={onGerarProxima} />
        ))}
      </CardContent>
    </Card>
  );
}

export default function ContasAPagar() {
  const [dados, setDados] = useState(null);
  const [loading, setLoading] = useState(true);

  const carregar = async () => {
    setLoading(true);
    try {
      const res = await despesasApi.getVencimentos();
      setDados(res.data);
    } catch {
      toast.error('Erro ao carregar vencimentos.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { carregar(); }, []);

  const gerarProxima = async (id) => {
    try {
      await despesasApi.gerarProxima(id);
      toast.success('Próxima parcela gerada com sucesso.');
      carregar();
    } catch (err) {
      toast.error(err.response?.data || 'Erro ao gerar próxima parcela.');
    }
  };

  if (loading) return <LoadingPage text="Carregando vencimentos..." />;

  const tudo = [
    ...(dados?.vencidas || []),
    ...(dados?.hoje || []),
    ...(dados?.proximos7Dias || []),
    ...(dados?.proximos30Dias || []),
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Contas a Pagar</h1>
          <p className="text-muted-foreground">Vencimentos pendentes nos próximos 30 dias.</p>
        </div>
        <Button variant="outline" onClick={carregar}>
          <RefreshCw className="h-4 w-4 mr-2" /> Atualizar
        </Button>
      </div>

      {/* Resumo */}
      {dados && (
        <div className="grid gap-4 md:grid-cols-4">
          <Card className="border-red-200">
            <CardHeader><CardTitle className="text-sm text-red-700">Vencidas</CardTitle></CardHeader>
            <CardContent className="text-2xl font-bold text-red-700">{formatCurrency(dados.totalVencido)}</CardContent>
          </Card>
          <Card className="border-amber-200">
            <CardHeader><CardTitle className="text-sm text-amber-700">Vence hoje</CardTitle></CardHeader>
            <CardContent className="text-2xl font-bold text-amber-700">{formatCurrency(dados.totalHoje)}</CardContent>
          </Card>
          <Card className="border-yellow-200">
            <CardHeader><CardTitle className="text-sm text-yellow-700">Próximos 7 dias</CardTitle></CardHeader>
            <CardContent className="text-2xl font-bold text-yellow-700">{formatCurrency(dados.totalProximos7Dias)}</CardContent>
          </Card>
          <Card>
            <CardHeader><CardTitle className="text-sm text-muted-foreground">Próximos 30 dias</CardTitle></CardHeader>
            <CardContent className="text-2xl font-bold">{formatCurrency(dados.totalProximos30Dias)}</CardContent>
          </Card>
        </div>
      )}

      {/* Seções */}
      {dados && (
        <>
          <Secao
            titulo="Vencidas"
            icon={AlertTriangle}
            cor="text-red-700"
            itens={dados.vencidas}
            total={dados.totalVencido}
            onGerarProxima={gerarProxima}
          />
          <Secao
            titulo="Vence hoje"
            icon={Clock}
            cor="text-amber-700"
            itens={dados.hoje}
            total={dados.totalHoje}
            onGerarProxima={gerarProxima}
          />
          <Secao
            titulo="Próximos 7 dias"
            icon={Calendar}
            cor="text-yellow-700"
            itens={dados.proximos7Dias}
            total={dados.totalProximos7Dias}
            onGerarProxima={gerarProxima}
          />
          <Secao
            titulo="Dias 8–30"
            icon={Calendar}
            cor="text-muted-foreground"
            itens={dados.proximos30Dias}
            total={dados.totalProximos30Dias}
            onGerarProxima={gerarProxima}
          />
          {tudo.length === 0 && (
            <Card>
              <CardContent className="flex flex-col items-center justify-center py-12 gap-3 text-muted-foreground">
                <CheckCircle className="h-10 w-10 text-green-500" />
                <p className="text-lg font-medium text-green-700">Nenhuma conta a pagar nos próximos 30 dias!</p>
              </CardContent>
            </Card>
          )}
        </>
      )}
    </div>
  );
}
