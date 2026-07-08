/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./**/*.razor",
    "./**/*.html",
    "./**/*.cs"
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ["Inter", "ui-sans-serif", "system-ui", "sans-serif"]
      },
      colors: {
        ink: {
          50: "#f8fafc",
          100: "#eef2f7",
          200: "#d9e2ec",
          300: "#bcccdc",
          400: "#829ab1",
          500: "#627d98",
          600: "#486581",
          700: "#334e68",
          800: "#243b53",
          900: "#102a43"
        },
        control: {
          50: "#f3f8f6",
          100: "#dceee7",
          500: "#2f855a",
          700: "#276749"
        }
      }
    }
  },
  plugins: []
}
