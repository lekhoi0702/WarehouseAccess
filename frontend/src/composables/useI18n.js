import { ref, computed } from 'vue';
import { T } from '../config/i18n';

const lang = ref(localStorage.getItem('lang') || 'zh');

export function useI18n() {
  const t = computed(() => T[lang.value]);

  function setLanguage(newLang) {
    if (T[newLang]) {
      lang.value = newLang;
      localStorage.setItem('lang', newLang);
    }
  }

  return {
    lang,
    t,
    setLanguage
  };
}
