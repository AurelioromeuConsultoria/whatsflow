/**
 * Tela exibida quando um erro de renderização não tratado borbulha até o
 * ErrorBoundary global. Evita a "tela branca" e dá ao usuário um caminho de saída.
 */
export function ErrorFallback() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-white px-6 text-center text-slate-800">
      <div className="w-full max-w-md">
        <h1 className="text-2xl font-bold text-[#1e4f82]">Algo deu errado</h1>
        <p className="mt-3 text-base leading-7 text-slate-500">
          Ocorreu um erro inesperado. Nossa equipe já foi notificada. Tente recarregar a página.
        </p>
        <button
          type="button"
          onClick={() => window.location.reload()}
          className="mt-6 inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-6 text-base font-bold text-white transition hover:bg-[#1d4ed8]"
        >
          Recarregar
        </button>
      </div>
    </div>
  );
}
