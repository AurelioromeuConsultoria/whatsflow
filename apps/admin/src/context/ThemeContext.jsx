import { createContext, useContext, useState, useEffect } from 'react';

const ThemeContext = createContext(null);
const THEME_STORAGE_KEY = 'admin-theme';
// Ordem do rodízio do botão de tema. WhatsFlow é o padrão (marca em primeiro).
const THEMES = ['whatsflow', 'light', 'dark'];
const DEFAULT_THEME = 'whatsflow';

function normalizeTheme(theme) {
  // Compatibilidade com o tema legado "verbo".
  if (theme === 'verbo') return 'whatsflow';
  return THEMES.includes(theme) ? theme : DEFAULT_THEME;
}

function applyTheme(theme) {
  const root = document.documentElement;
  root.classList.toggle('dark', theme === 'dark');
  root.dataset.theme = theme;
}

export function ThemeProvider({ children }) {
  const [theme, setTheme] = useState(DEFAULT_THEME);

  useEffect(() => {
    const savedTheme = normalizeTheme(localStorage.getItem(THEME_STORAGE_KEY));
    setTheme(savedTheme);
    applyTheme(savedTheme);
  }, []);

  const updateTheme = (newTheme) => {
    const nextTheme = normalizeTheme(newTheme);
    setTheme(nextTheme);
    localStorage.setItem(THEME_STORAGE_KEY, nextTheme);
    applyTheme(nextTheme);
  };

  const toggleTheme = () => {
    // Rodízio entre os três temas: whatsflow → light → dark → whatsflow.
    const currentIndex = THEMES.indexOf(theme);
    const nextTheme = THEMES[(currentIndex + 1) % THEMES.length];
    updateTheme(nextTheme);
  };

  const value = {
    theme,
    setTheme: updateTheme,
    isDark: theme === 'dark',
    isWhatsflow: theme === 'whatsflow',
    // Alias legado para componentes que ainda referenciam isVerbo.
    isVerbo: theme === 'whatsflow',
    toggleTheme,
    themes: THEMES,
  };

  return (
    <ThemeContext.Provider value={value}>
      {children}
    </ThemeContext.Provider>
  );
}

export function useTheme() {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error('useTheme deve ser usado dentro de ThemeProvider');
  }
  return context;
}
