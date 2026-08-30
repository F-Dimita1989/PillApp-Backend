-- Indici necessari alle query di sola lettura su farmaci_classe_a (PostgreSQL / Supabase).
-- Eseguire una volta nel SQL Editor di Supabase.

-- 1. Lookup per codice AIC: GET /api/farmaci/{aic}
-- Senza questo indice ogni lookup esegue una scansione completa della tabella.
-- Se il vincolo di unicità fallisce significa che in tabella ci sono AIC duplicati:
-- in quel caso individuarli con la query in fondo al file e ripulirli prima di riprovare.
CREATE UNIQUE INDEX IF NOT EXISTS idx_farmaci_classe_a_aic
    ON farmaci_classe_a (aic);

-- 2. Ordinamento della ricerca: ORDER BY denominazione_confezione, aic
CREATE INDEX IF NOT EXISTS idx_farmaci_classe_a_denominazione_ordinamento
    ON farmaci_classe_a (denominazione_confezione, aic);

-- 3. Ricerca testuale ILIKE '%termine%': richiede indici trigram.
-- Diventano efficaci da 3 caratteri, per questo l'API rifiuta termini più corti.
CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE INDEX IF NOT EXISTS idx_farmaci_classe_a_principio_attivo_trgm
    ON farmaci_classe_a USING gin (principio_attivo gin_trgm_ops);

CREATE INDEX IF NOT EXISTS idx_farmaci_classe_a_denominazione_trgm
    ON farmaci_classe_a USING gin (denominazione_confezione gin_trgm_ops);

CREATE INDEX IF NOT EXISTS idx_farmaci_classe_a_descrizione_gruppo_trgm
    ON farmaci_classe_a USING gin (descrizione_gruppo gin_trgm_ops);

-- Diagnostica: elenca eventuali codici AIC duplicati.
-- SELECT aic, count(*) FROM farmaci_classe_a GROUP BY aic HAVING count(*) > 1;
