import { Check, X } from 'lucide-react';
import { avaliarSenha } from '@/lib/passwordPolicy';

/**
 * Checklist de requisitos da senha, com feedback verde/cinza conforme o usuário digita.
 * Não renderiza nada enquanto a senha estiver vazia.
 */
export function PasswordRequirements({ senha }) {
  if (!senha) return null;

  const { requisitos } = avaliarSenha(senha);

  return (
    <ul className="mt-1 space-y-1 text-xs">
      {requisitos.map((r) => (
        <li
          key={r.label}
          className={`flex items-center gap-1.5 ${r.ok ? 'text-emerald-600' : 'text-slate-400'}`}
        >
          {r.ok ? <Check className="size-3.5" /> : <X className="size-3.5" />}
          {r.label}
        </li>
      ))}
    </ul>
  );
}
