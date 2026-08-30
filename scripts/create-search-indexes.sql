-- Indici richiesti dalle query di sola lettura su farmaci_classe_a (PostgreSQL / Supabase).
-- Eseguire nel SQL Editor di Supabase.
--
-- L'API esegue due sole query:
--   1. GET /api/farmaci/{aic}      → WHERE aic = $1
--   2. GET /api/farmaci/search?q=  → WHERE principio_attivo ILIKE '%q%'
--                                       OR denominazione_confezione ILIKE '%q%'
--                                       OR descrizione_gruppo ILIKE '%q%'
--                                     ORDER BY denominazione_confezione, aic
--
-- Tutto ciò che non serve a queste due query è peso morto: occupa spazio
-- (500 MB totali sul piano gratuito) e va mantenuto a ogni scrittura.

-- ---------------------------------------------------------------------------
-- PASSO 1 — Ricognizione: cosa esiste già, quanto occupa, quanto viene usato
-- ---------------------------------------------------------------------------
-- La colonna volte_usato conta le letture dell'indice da parte del planner:
-- un indice fermo a zero non sta servendo a nulla.

SELECT
    c.relname AS indice,
    pg_size_pretty(pg_relation_size(c.oid)) AS dimensione,
    s.idx_scan AS volte_usato,
    pg_get_indexdef(c.oid) AS definizione
FROM pg_class c
JOIN pg_index x ON x.indexrelid = c.oid
JOIN pg_class t ON t.oid = x.indrelid
LEFT JOIN pg_stat_user_indexes s ON s.indexrelid = c.oid
WHERE t.relname = 'farmaci_classe_a'
ORDER BY pg_relation_size(c.oid) DESC;

-- ---------------------------------------------------------------------------
-- PASSO 2 — Lookup per codice AIC
-- ---------------------------------------------------------------------------
-- NON serve creare nulla se sulla colonna aic esiste un vincolo UNIQUE:
-- PostgreSQL crea automaticamente l'indice che lo sostiene (di norma chiamato
-- farmaci_classe_a_aic_key) e il lookup per AIC lo usa già.
--
-- Verifica:
--   SELECT conname, contype FROM pg_constraint
--   WHERE conrelid = 'farmaci_classe_a'::regclass;
--
-- Solo se il vincolo NON esiste, creare l'indice:
--   CREATE UNIQUE INDEX IF NOT EXISTS idx_farmaci_classe_a_aic
--       ON farmaci_classe_a (aic);
--
-- Se la creazione fallisce ci sono AIC duplicati in tabella. Individuarli con:
--   SELECT aic, count(*) FROM farmaci_classe_a GROUP BY aic HAVING count(*) > 1;

-- ---------------------------------------------------------------------------
-- PASSO 3 — Ordinamento della ricerca
-- ---------------------------------------------------------------------------
-- Corrisponde a ORDER BY denominazione_confezione, aic. Con un indice già
-- ordinato su quelle due colonne il database non deve ordinare in memoria a
-- ogni richiesta.

CREATE INDEX IF NOT EXISTS idx_farmaci_classe_a_denominazione_ordinamento
    ON farmaci_classe_a (denominazione_confezione, aic);

-- ---------------------------------------------------------------------------
-- PASSO 4 — Ricerca testuale ILIKE '%termine%'
-- ---------------------------------------------------------------------------
-- Un indice b-tree non è utilizzabile quando non si sa come inizia il testo
-- cercato. Servono gli indici trigram, che spezzano il testo in gruppi di tre
-- caratteri: da qui il minimo di 3 caratteri imposto dall'API.
--
-- ATTENZIONE ai nomi: se sulla stessa colonna esiste già un indice trigram con
-- un nome diverso, IF NOT EXISTS non lo rileva e ne crea un secondo identico.
-- Controllare l'output del PASSO 1 prima di eseguire questo blocco.

CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE INDEX IF NOT EXISTS idx_farmaci_classe_a_principio_attivo_trgm
    ON farmaci_classe_a USING gin (principio_attivo gin_trgm_ops);

CREATE INDEX IF NOT EXISTS idx_farmaci_classe_a_denominazione_trgm
    ON farmaci_classe_a USING gin (denominazione_confezione gin_trgm_ops);

CREATE INDEX IF NOT EXISTS idx_farmaci_classe_a_descrizione_gruppo_trgm
    ON farmaci_classe_a USING gin (descrizione_gruppo gin_trgm_ops);

-- ---------------------------------------------------------------------------
-- PASSO 5 — Pulizia degli indici ridondanti
-- ---------------------------------------------------------------------------
-- Rimuove gli indici che il PASSO 1 ha mostrato essere duplicati o inutilizzati
-- (idx_scan a zero mentre altri indici della stessa tabella registravano
-- letture, quindi non è un problema di statistiche azzerate).

-- Secondo indice trigram su principio_attivo, con definizione identica a
-- idx_farmaci_classe_a_principio_attivo_trgm. Si tiene quello ricreato dal
-- PASSO 4 perché appena costruito e più compatto (464 kB contro 2528 kB: un
-- indice GIN accumula spazio sprecato dopo molti aggiornamenti).
DROP INDEX IF EXISTS idx_farmaci_classe_a_principio_trgm;

-- Doppione dell'indice che sostiene il vincolo UNIQUE su aic. Va rimosso
-- questo e non farmaci_classe_a_aic_key, che il vincolo richiede.
DROP INDEX IF EXISTS idx_farmaci_classe_a_aic;

-- B-tree che l'API non può usare: la ricerca è ILIKE '%...%', e queste colonne
-- non compaiono in nessun filtro di uguaglianza né nell'ordinamento.
DROP INDEX IF EXISTS idx_farmaci_classe_a_principio_attivo;
DROP INDEX IF EXISTS idx_farmaci_classe_a_titolare_aic;
DROP INDEX IF EXISTS idx_farmaci_classe_a_codice_gruppo_equivalenza;

-- NON rimuovere farmaci_classe_a_pkey né farmaci_classe_a_aic_key: sostengono
-- la chiave primaria e il vincolo di unicità. Il loro idx_scan può essere zero
-- senza che questo li renda superflui.

-- ---------------------------------------------------------------------------
-- PASSO 6 — Verificare che gli indici trigram vengano davvero usati
-- ---------------------------------------------------------------------------
-- La query di ricerca ha tre condizioni in OR su tre colonne: il planner deve
-- combinare tre scansioni di indice, e su una tabella di queste dimensioni può
-- preferire la scansione completa. In quel caso i tre indici GIN occupano
-- spazio senza dare nulla in cambio.
--
-- Se nel piano compaiono Bitmap Index Scan sugli indici *_trgm stanno
-- lavorando; se compare Seq Scan on farmaci_classe_a, no.

EXPLAIN ANALYZE
SELECT aic, principio_attivo, denominazione_confezione
FROM farmaci_classe_a
WHERE principio_attivo ILIKE '%paracetamolo%'
   OR denominazione_confezione ILIKE '%paracetamolo%'
   OR descrizione_gruppo ILIKE '%paracetamolo%'
ORDER BY denominazione_confezione, aic
LIMIT 20;
