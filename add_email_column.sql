-- Dodanie kolumny email do tabeli user
ALTER TABLE "user" ADD COLUMN email VARCHAR(255);

-- Opcjonalnie: dodanie ograniczenia UNIQUE dla email
ALTER TABLE "user" ADD CONSTRAINT unique_email UNIQUE (email);

-- Opcjonalnie: dodanie ograniczenia NOT NULL (tylko jeśli wszystkie istniejące rekordy mają email)
-- ALTER TABLE "user" ALTER COLUMN email SET NOT NULL;

-- Sprawdzenie struktury tabeli po zmianach
SELECT column_name, data_type, is_nullable, column_default 
FROM information_schema.columns 
WHERE table_name = 'user' 
ORDER BY ordinal_position;
