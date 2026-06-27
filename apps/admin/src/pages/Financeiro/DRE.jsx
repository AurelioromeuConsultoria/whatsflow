import { useEffect, useState } from 'react';
import { Download, ChevronDown, ChevronUp, TrendingUp, TrendingDown, Minus } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { LoadingPage } from '@/components/ui/loading';
import { relatoriosFinanceirosApi } from '@/lib/api';
import { toast } from 'sonner';
import { formatCurrency } from '@/lib/formatters';

const anoAtual = new Date().getFullYear();
const ANOS = Array.from({ length: 5 }, (_, i) => anoAtual - 2 + i);

function ResultadoBadge({ valor }) {
  if (valor > 0) return <Badge className="bg-green-100 text-green-800 border-green-200">+{formatCurrency(valor)}</Badge>;
  if (valor < 0) return <Badge className="bg-red-100 text-red-800 border-red-200">{formatCurrency(valor)}</Badge>;
  return <Badge variant="secondary">{formatCurrency(0)}</Badge>;
}

function MesRow({ mes, expandido, onToggle }) {
  const resultado = mes.totalReceitas - mes.totalDespesas;
  const temMovimento = mes.totalReceitas > 0 || mes.totalDespesas > 0;

  return (
    <>
      <tr
        className={`border-b transition-colors ${temMovimento ? 'cursor-pointer hover:bg-muted/40' : 'opacity-40'}`}
        onClick={temMovimento ? onToggle : undefined}
      >
        <td className="px-4 py-3 font-medium flex items-center gap-2">
          {temMovimento && (
            expandido ? <ChevronUp className="h-4 w-4 text-muted-foreground" /> : <ChevronDown className="h-4 w-4 text-muted-foreground" />
          )}
          {mes.mesNome}
        </td>
        <td className="px-4 py-3 text-right text-green-700 font-semibold">
          {mes.totalReceitas > 0 ? formatCurrency(mes.totalReceitas) : '-'}
        </td>
        <td className="px-4 py-3 text-right text-red-700 font-semibold">
          {mes.totalDespesas > 0 ? formatCurrency(mes.totalDespesas) : '-'}
        </td>
        <td className="px-4 py-3 text-right">
          {temMovimento ? <ResultadoBadge valor={resultado} /> : '-'}
        </td>
      </tr>
      {expandido && temMovimento && (
        <tr className="border-b bg-muted/10">
          <td colSpan={4} className="px-6 py-3">
            <div className="grid gap-4 md:grid-cols-2 text-sm">
              {mes.receitas.length > 0 && (
                <div>
                  <p className="font-semibold text-green-700 mb-2 flex items-center gap-1">
                    <TrendingUp className="h-3.5 w-3.5" /> Receitas por categoria
                  </p>
                  <div className="space-y-1">
                    {mes.receitas.map((c, i) => (
                      <div key={i} className="flex items-center justify-between">
                        <span className="text-muted-foreground">{c.categoriaNome}</span>
                        <span>{formatCurrency(c.valor)} <span className="text-xs text-muted-foreground">({c.quantidade}x)</span></span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
              {mes.despesas.length > 0 && (
                <div>
                  <p className="font-semibold text-red-700 mb-2 flex items-center gap-1">
                    <TrendingDown className="h-3.5 w-3.5" /> Despesas por categoria
                  </p>
                  <div className="space-y-1">
                    {mes.despesas.map((c, i) => (
                      <div key={i} className="flex items-center justify-between">
                        <span className="text-muted-foreground">{c.categoriaNome}</span>
                        <span>{formatCurrency(c.valor)} <span className="text-xs text-muted-foreground">({c.quantidade}x)</span></span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </td>
        </tr>
      )}
    </>
  );
}

export default function DRE() {
  const [ano, setAno] = useState(anoAtual);
  const [dre, setDre] = useState(null);
  const [loading, setLoading] = useState(false);
  const [expandidos, setExpandidos] = useState({});

  const carregar = async (anoSel) => {
    setLoading(true);
    setExpandidos({});
    try {
      const res = await relatoriosFinanceirosApi.getDre(anoSel);
      setDre(res.data);
    } catch {
      toast.error('Erro ao carregar DRE.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { carregar(ano); }, [ano]);

  const toggleMes = (mes) => setExpandidos((prev) => ({ ...prev, [mes]: !prev[mes] }));

  const exportarCSV = () => {
    if (!dre) return;
    const linhas = [
      ['Mês', 'Receitas', 'Despesas', 'Resultado'],
      ...dre.meses.map((m) => [
        m.mesNome,
        m.totalReceitas.toFixed(2),
        m.totalDespesas.toFixed(2),
        (m.totalReceitas - m.totalDespesas).toFixed(2),
      ]),
      ['TOTAL', dre.totalReceitas.toFixed(2), dre.totalDespesas.toFixed(2), dre.resultado.toFixed(2)],
    ];
    const csv = linhas.map((l) => l.join(';')).join('\n');
    const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `dre_${ano}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h1 className="text-3xl font-bold">DRE — Demonstrativo de Resultado</h1>
          <p className="text-muted-foreground">Receitas, despesas e resultado líquido por mês.</p>
        </div>
        <div className="flex items-center gap-3">
          <select
            value={ano}
            onChange={(e) => setAno(Number(e.target.value))}
            className="px-3 py-2 border rounded text-sm"
          >
            {ANOS.map((a) => <option key={a} value={a}>{a}</option>)}
          </select>
          <button
            type="button"
            onClick={exportarCSV}
            disabled={!dre}
            className="flex items-center gap-2 px-3 py-2 border rounded text-sm hover:bg-muted disabled:opacity-50"
          >
            <Download className="h-4 w-4" /> CSV
          </button>
        </div>
      </div>

      {loading && <LoadingPage text="Carregando DRE..." />}

      {dre && !loading && (
        <>
          {/* Cards de resumo */}
          <div className="grid gap-4 md:grid-cols-3">
            <Card className="border-green-200">
              <CardHeader><CardTitle className="text-sm text-green-700 flex items-center gap-2"><TrendingUp className="h-4 w-4" /> Total de Receitas</CardTitle></CardHeader>
              <CardContent className="text-2xl font-bold text-green-700">{formatCurrency(dre.totalReceitas)}</CardContent>
            </Card>
            <Card className="border-red-200">
              <CardHeader><CardTitle className="text-sm text-red-700 flex items-center gap-2"><TrendingDown className="h-4 w-4" /> Total de Despesas</CardTitle></CardHeader>
              <CardContent className="text-2xl font-bold text-red-700">{formatCurrency(dre.totalDespesas)}</CardContent>
            </Card>
            <Card className={dre.resultado >= 0 ? 'border-green-200' : 'border-red-200'}>
              <CardHeader><CardTitle className={`text-sm flex items-center gap-2 ${dre.resultado >= 0 ? 'text-green-700' : 'text-red-700'}`}><Minus className="h-4 w-4" /> Resultado do Exercício</CardTitle></CardHeader>
              <CardContent className={`text-2xl font-bold ${dre.resultado >= 0 ? 'text-green-700' : 'text-red-700'}`}>
                {dre.resultado >= 0 ? '+' : ''}{formatCurrency(dre.resultado)}
              </CardContent>
            </Card>
          </div>

          {/* Tabela mensal */}
          <Card>
            <CardHeader>
              <CardTitle>Resultado mensal {ano}</CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="px-4 py-2 text-left font-medium">Mês</th>
                    <th className="px-4 py-2 text-right font-medium text-green-700">Receitas</th>
                    <th className="px-4 py-2 text-right font-medium text-red-700">Despesas</th>
                    <th className="px-4 py-2 text-right font-medium">Resultado</th>
                  </tr>
                </thead>
                <tbody>
                  {dre.meses.map((mes) => (
                    <MesRow
                      key={mes.mes}
                      mes={mes}
                      expandido={!!expandidos[mes.mes]}
                      onToggle={() => toggleMes(mes.mes)}
                    />
                  ))}
                  {/* Linha de total */}
                  <tr className="border-t-2 bg-muted/30 font-bold">
                    <td className="px-4 py-3">TOTAL {ano}</td>
                    <td className="px-4 py-3 text-right text-green-700">{formatCurrency(dre.totalReceitas)}</td>
                    <td className="px-4 py-3 text-right text-red-700">{formatCurrency(dre.totalDespesas)}</td>
                    <td className="px-4 py-3 text-right"><ResultadoBadge valor={dre.resultado} /></td>
                  </tr>
                </tbody>
              </table>
            </CardContent>
          </Card>

          {/* Consolidado por categoria */}
          {(dre.totalPorCategoriaReceita.length > 0 || dre.totalPorCategoriaDespesa.length > 0) && (
            <div className="grid gap-4 md:grid-cols-2">
              {dre.totalPorCategoriaReceita.length > 0 && (
                <Card>
                  <CardHeader><CardTitle className="text-sm text-green-700">Receitas por categoria — {ano}</CardTitle></CardHeader>
                  <CardContent className="space-y-2">
                    {dre.totalPorCategoriaReceita.map((c, i) => (
                      <div key={i} className="flex items-center justify-between text-sm border-b pb-1">
                        <span className="text-muted-foreground">{c.categoriaNome}</span>
                        <div className="text-right">
                          <span className="font-semibold">{formatCurrency(c.valor)}</span>
                          <span className="text-xs text-muted-foreground ml-2">{c.quantidade}x</span>
                        </div>
                      </div>
                    ))}
                  </CardContent>
                </Card>
              )}
              {dre.totalPorCategoriaDespesa.length > 0 && (
                <Card>
                  <CardHeader><CardTitle className="text-sm text-red-700">Despesas por categoria — {ano}</CardTitle></CardHeader>
                  <CardContent className="space-y-2">
                    {dre.totalPorCategoriaDespesa.map((c, i) => (
                      <div key={i} className="flex items-center justify-between text-sm border-b pb-1">
                        <span className="text-muted-foreground">{c.categoriaNome}</span>
                        <div className="text-right">
                          <span className="font-semibold">{formatCurrency(c.valor)}</span>
                          <span className="text-xs text-muted-foreground ml-2">{c.quantidade}x</span>
                        </div>
                      </div>
                    ))}
                  </CardContent>
                </Card>
              )}
            </div>
          )}
        </>
      )}
    </div>
  );
}
