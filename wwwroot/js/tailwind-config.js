// Tailwind CSS Configuration
tailwind.config = {
    darkMode: "class",
    theme: {
        extend: {
            colors: {
                "primary": "#135bec",
                "primary-dark": "#0f4ac7",
                "background-light": "#f6f6f8",
                "background-dark": "#101622",
                "surface-light": "#ffffff",
                "surface-dark": "#1e293b",
                "border-light": "#e2e8f0",
                "border-dark": "#334155",
            },
            fontFamily: {
                "display": ["Inter", "system-ui", "sans-serif"]
            },
            borderRadius: {
                "DEFAULT": "0.5rem",
                "lg": "0.75rem",
                "xl": "1rem",
                "2xl": "1.25rem",
                "full": "9999px"
            },
            boxShadow: {
                'soft': '0 2px 8px 0 rgba(0, 0, 0, 0.08)',
                'medium': '0 4px 12px 0 rgba(0, 0, 0, 0.12)',
                'strong': '0 8px 24px 0 rgba(0, 0, 0, 0.16)',
            },
            animation: {
                'fade-in': 'fadeIn 0.3s ease-in-out',
                'slide-up': 'slideUp 0.3s ease-out',
                'bounce-subtle': 'bounceSubtle 2s infinite',
            },
            keyframes: {
                fadeIn: {
                    '0%': { opacity: '0' },
                    '100%': { opacity: '1' },
                },
                slideUp: {
                    '0%': { 
                        opacity: '0',
                        transform: 'translateY(10px)' 
                    },
                    '100%': { 
                        opacity: '1',
                        transform: 'translateY(0)' 
                    },
                },
                bounceSubtle: {
                    '0%, 100%': { 
                        transform: 'translateY(0)' 
                    },
                    '50%': { 
                        transform: 'translateY(-2px)' 
                    },
                },
            },
            spacing: {
                '18': '4.5rem',
                '88': '22rem',
                '128': '32rem',
            },
            fontSize: {
                '2xs': ['0.625rem', { lineHeight: '0.75rem' }],
                '3xl': ['1.875rem', { lineHeight: '2.25rem' }],
                '4xl': ['2.25rem', { lineHeight: '2.5rem' }],
            },
            zIndex: {
                '60': '60',
                '70': '70',
                '80': '80',
                '90': '90',
                '100': '100',
            }
        },
    },
    plugins: []
};
