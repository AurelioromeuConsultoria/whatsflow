import { useState } from 'react';
import { Download, FileText, ChevronDown, ChevronUp, AlertCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { LoadingPage } from '@/components/ui/loading';
import { receitasApi, categoriasReceitasApi } from '@/lib/api';
import { toast } from 'sonner';
import { formatCurrency, formatDate } from '@/lib/formatters';

function InformeModal({ pessoaId, pessoaNome, onClose }) {
  const anoAtual = new Date().getFullYear();
  const [ano, setAno] = useState(String(anoAtual));
  const [informe, setInforme] = useState(null);
  const [loading, setLoading] = useState(false);

  const buscar = async () => {
    setLoading(true);
    try {
      const res = await receitasApi.getInformeAnual(pessoaId, Number(ano));
      setInforme(res.data);
    } catch {
      toast.error('Erro ao gerar informe.');
    } finally {
      setLoading(false);
    }
  };

  const imprimir = () => window.print();

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="w-full max-w-2xl rounded-lg bg-white shadow-xl max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between border-b p-4 print:hidden">
          <h2 className="text-lg font-semibold">Informe de Contribuições — {pessoaNome}</h2>
          <button onClick={onClose} className="text-muted-foreground hover:text-foreground text-xl leading-none">×</button>
        </div>
        <div className="p-4 space-y-4 print:hidden">
          <div className="flex items-end gap-3">
            <div className="space-y-1 flex-1">
              <Label>Ano</Label>
              <Input
                type="number"
                value={ano}
                onChange={(e) => setAno(e.target.value)}
                min="2020"
                max={anoAtual}
              />
            </div>
            <Button onClick={buscar} disabled={loading}>
              {loading ? 'Gerando...' : 'Gerar'}
            </Button>
          </div>
        </div>

        {informe && (
          <div className="p-6 space-y-6" id="informe-print">
            {/* Cabeçalho imprimível */}
            <div className="text-center border-b pb-4">
              <h3 className="text-xl font-bold">Informe de Contribuições</h3>
              <p className="text-muted-foreground text-sm">Ano {informe.ano}</p>
            </div>
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <span className="text-muted-foreground">Contribuinte:</span>
                <p className="font-semibold">{informe.pessoaNome}</p>
              </div>
              {informe.pessoaEmail && (
                <div>
                  <span className="text-muted-foreground">E-mail:</span>
                  <p className="font-semibold">{informe.pessoaEmail}</p>
                </div>
              )}
              <div>
                <span className="text-muted-foreground">Total no ano:</span>
                <p className="text-xl font-bold text-green-700">{formatCurrency(informe.totalAnual)}</p>
              </div>
              <div>
                <span className="text-muted-foreground">Data de emissão:</span>
                <p className="font-semibold">{formatDate(informe.dataEmissao)}</p>
              </div>
            </div>

            <div>
              <h4 className="font-semibold mb-2">Por mês</h4>
              <div className="grid grid-cols-3 gap-2 text-sm">
                {informe.porMes.filter((m) => m.total > 0).map((m) => (
                  <div key={m.mes} className="rounded border p-2">
                    <div className="text-muted-foreground text-xs">{m.mesNome}</div>
                    <div className="font-semibold">{formatCurrency(m.total)}</div>
                    <div className="text-xs text-muted-foreground">{m.quantidade} lançamento(s)</div>
                  </div>
                ))}
              </div>
            </div>

            {informe.porCategoria.length > 0 && (
              <div>
                <h4 className="font-semibold mb-2">Por categoria</h4>
                <div className="space-y-1">
                  {informe.porCategoria.map((c, i) => (
                    <div key={i} className="flex items-center justify-between text-sm border-b py-1">
                      <span>{c.categoriaNome}</span>
                      <span className="font-semibold">{formatCurrency(c.total)}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}

            <div className="flex gap-2 print:hidden">
              <Button size="sm" onClick={imprimir}>
                <FileText className="h-4 w-4 mr-2" /> Imprimir / Salvar PDF
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

export default function RelatorioContribuicoes() {
  const hoje = new Date();
  const primeiroDiaMes = new Date(hoje.getFullYear(), hoje.getMonth(), 1).toISOString().slice(0, 10);

  const [filtros, setFiltros] = useState({
    dataInicio: primeiroDiaMes,
    dataFim: hoje.toISOString().slice(0, 10),
    categoriaId: '',
    incluirSemContribuicao: true,
  });
  const [categorias, setCategorias] = useState([]);
  const [relatorio, setRelatorio] = useState(null);
  const [loading, setLoading] = useState(false);
  const [expandidos, setExpandidos] = useState({});
  const [informeAberto, setInformeAberto] = useState(null);

  useState(() => {
    categoriasReceitasApi.getAll()
      .then((res) => setCategorias((res.data || []).filter((c) => c.ativo)))
      .catch(console.error);
  }, []);

  const buscar = async () => {
    if (!filtros.dataInicio || !filtros.dataFim) {
      toast.error('Informe o período.');
      return;
    }
    setLoading(true);
    try {
      const params = {
        dataInicio: new Date(filtros.dataInicio).toISOString(),
        dataFim: new Date(filtros.dataFim + 'T23:59:59').toISOString(),
        categoriaId: filtros.categoriaId || undefined,
      };
      const res = await receitasApi.getRelatorioContribuicoes(params);
      setRelatorio(res.data);
    } catch {
      toast.error('Erro ao gerar relatório.');
    } finally {
      setLoading(false);
    }
  };

  const exportarCSV = () => {
    if (!relatorio) return;
    const linhas = [
      ['Nome', 'Total', 'Lançamentos', 'Última contribuição'],
      ...relatorio.contribuidores.map((c) => [
        c.pessoaNome,
        c.total.toFixed(2),
        c.quantidadeLancamentos,
        c.ultimaContribuicao ? formatDate(c.ultimaContribuicao) : '-',
      ]),
    ];
    const csv = linhas.map((l) => l.join(';')).join('\n');
    const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `contribuicoes_${filtros.dataInicio}_${filtros.dataFim}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const toggleExpandir = (pessoaId) =>
    setExpandidos((prev) => ({ ...prev, [pessoaId]: !prev[pessoaId] }));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Relatório de Contribuições</h1>
        <p className="text-muted-foreground">Histórico de dízimos e ofertas por membro no período.</p>
      </div>

      {/* Filtros */}
      <Card>
        <CardHeader><CardTitle>Filtros</CardTitle></CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-4">
            <div className="space-y-2">
              <Label>Data início</Label>
              <Input type="date" value={filtros.dataInicio} onChange={(e) => setFiltros((p) => ({ ...p, dataInicio: e.target.value }))} />
            </div>
            <div className="space-y-2">
              <Label>Data fim</Label>
              <Input type="date" value={filtros.dataFim} onChange={(e) => setFiltros((p) => ({ ...p, dataFim: e.target.value }))} />
            </div>
            <div className="space-y-2">
              <Label>Categoria</Label>
              <select
                value={filtros.categoriaId}
                onChange={(e) => setFiltros((p) => ({ ...p, categoriaId: e.target.value }))}
                className="w-full px-3 py-2 border rounded"
              >
                <option value="">Todas</option>
                {categorias.map((c) => <option key={c.id} value={c.id}>{c.nome}</option>)}
              </select>
            </div>
            <div className="flex items-end">
              <Button onClick={buscar} disabled={loading} className="w-full">
                {loading ? 'Carregando...' : 'Gerar relatório'}
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {loading && <LoadingPage text="Gerando relatório..." />}

      {relatorio && !loading && (
        <>
          {/* Resumo */}
          <div className="grid gap-4 md:grid-cols-4">
            <Card>
              <CardHeader><CardTitle className="text-sm">Total arrecadado</CardTitle></CardHeader>
              <CardContent className="text-2xl font-bold text-green-700">{formatCurrency(relatorio.totalGeral)}</CardContent>
            </Card>
            <Card>
              <CardHeader><CardTitle className="text-sm">Lançamentos</CardTitle></CardHeader>
              <CardContent className="text-2xl font-bold">{relatorio.totalLancamentos}</CardContent>
            </Card>
            <Card>
              <CardHeader><CardTitle className="text-sm">Membros que contribuíram</CardTitle></CardHeader>
              <CardContent className="text-2xl font-bold text-green-600">{relatorio.totalMembrosContribuiram}</CardContent>
            </Card>
            <Card>
              <CardHeader><CardTitle className="text-sm">Sem contribuição</CardTitle></CardHeader>
              <CardContent className="text-2xl font-bold text-amber-600">{relatorio.totalMembrosSemContribuicao}</CardContent>
            </Card>
          </div>

          {/* Lista de contribuidores */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center justify-between">
                <span>Contribuidores ({relatorio.contribuidores.length})</span>
                <Button variant="outline" size="sm" onClick={exportarCSV}>
                  <Download className="h-4 w-4 mr-2" /> Exportar CSV
                </Button>
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              {relatorio.contribuidores.map((c) => (
                <div key={c.pessoaId} className="rounded-lg border overflow-hidden">
                  <button
                    type="button"
                    onClick={() => toggleExpandir(c.pessoaId)}
                    className="flex w-full items-center justify-between px-4 py-3 text-left hover:bg-muted/50"
                  >
                    <div className="flex items-center gap-3">
                      <span className="font-medium">{c.pessoaNome}</span>
                      <Badge variant="secondary">{c.quantidadeLancamentos} lançamento(s)</Badge>
                    </div>
                    <div className="flex items-center gap-3">
                      <span className="font-bold text-green-700">{formatCurrency(c.total)}</span>
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={(e) => { e.stopPropagation(); setInformeAberto({ id: c.pessoaId, nome: c.pessoaNome }); }}
                        className="text-xs"
                      >
                        <FileText className="h-3.5 w-3.5 mr-1" /> Informe
                      </Button>
                      {expandidos[c.pessoaId] ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
                    </div>
                  </button>
                  {expandidos[c.pessoaId] && (
                    <div className="border-t bg-muted/20 px-4 py-3">
                      <div className="grid gap-2 md:grid-cols-2">
                        {c.porCategoria.map((cat, i) => (
                          <div key={i} className="flex items-center justify-between text-sm">
                            <span className="text-muted-foreground">{cat.categoriaNome}</span>
                            <span>{formatCurrency(cat.total)} ({cat.quantidade}x)</span>
                          </div>
                        ))}
                      </div>
                      {c.ultimaContribuicao && (
                        <p className="mt-2 text-xs text-muted-foreground">
                          Última: {formatDate(c.ultimaContribuicao)}
                        </p>
                      )}
                    </div>
                  )}
                </div>
              ))}
              {relatorio.contribuidores.length === 0 && (
                <p className="text-center text-muted-foreground py-8">Nenhum membro identificado contribuiu neste período.</p>
              )}
            </CardContent>
          </Card>

          {/* Membros sem contribuição */}
          {relatorio.semContribuicao.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2 text-amber-700">
                  <AlertCircle className="h-5 w-5" />
                  Sem contribuição no período ({relatorio.semContribuicao.length})
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid gap-2 md:grid-cols-2 lg:grid-cols-3">
                  {relatorio.semContribuicao.map((m) => (
                    <div key={m.pessoaId} className="flex items-center justify-between rounded border px-3 py-2 text-sm">
                      <span>{m.pessoaNome}</span>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => setInformeAberto({ id: m.pessoaId, nome: m.pessoaNome })}
                        className="text-xs h-7"
                      >
                        <FileText className="h-3 w-3 mr-1" /> Informe
                      </Button>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}
        </>
      )}

      {informeAberto && (
        <InformeModal
          pessoaId={informeAberto.id}
          pessoaNome={informeAberto.nome}
          onClose={() => setInformeAberto(null)}
        />
      )}
    </div>
  );
}
