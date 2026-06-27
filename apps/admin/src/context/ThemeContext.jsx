import { createContext, useContext, useState, useEffect } from 'react';

const ThemeContext = createContext(null);
const THEME_STORAGE_KEY = 'admin-theme';
// Ordem do rodízio do botão de tema. Verbo+ é o padrão (marca em primeiro).
const THEMES = ['verbo', 'light', 'dark'];
const DEFAULT_THEME = 'verbo';

function normalizeTheme(theme) {
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
    // Rodízio entre os três temas: verbo → light → dark → verbo.
    const currentIndex = THEMES.indexOf(theme);
    const nextTheme = THEMES[(currentIndex + 1) % THEMES.length];
    updateTheme(nextTheme);
  };

  const value = {
    theme,
    setTheme: updateTheme,
    isDark: theme === 'dark',
    isVerbo: theme === 'verbo',
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
