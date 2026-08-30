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
-- Corrisponde a ORDER BY denominazione_confezione, aic.
--
-- Utilità da verificare: con un termine selettivo il filtro trigram restituisce
-- poche righe e il planner le ordina in memoria (quicksort), ignorando questo
-- indice. Paga solo nello scenario opposto, quando il termine corrisponde a
-- molte righe e conviene scorrere un indice già ordinato fermandosi al LIMIT.
-- Vedi il PASSO 6.

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
-- PASSO 6 — Verificare quali indici il planner sceglie davvero
-- ---------------------------------------------------------------------------
-- Un indice esiste solo in funzione di una query: il modo per saperlo è leggere
-- il piano di esecuzione, non fidarsi del ragionamento.

-- 6a. Termine selettivo.
-- Verificato: il planner usa tutti e tre gli indici trigram combinati con
-- BitmapOr (nessun Seq Scan), legge 5 blocchi e conclude in circa 7 ms.
-- L'ORDER BY viene risolto in memoria con quicksort su poche righe, quindi
-- idx_farmaci_classe_a_denominazione_ordinamento NON viene usato qui.

EXPLAIN ANALYZE
SELECT aic, principio_attivo, denominazione_confezione
FROM farmaci_classe_a
WHERE principio_attivo ILIKE '%paracetamolo%'
   OR denominazione_confezione ILIKE '%paracetamolo%'
   OR descrizione_gruppo ILIKE '%paracetamolo%'
ORDER BY denominazione_confezione, aic
LIMIT 20;

-- 6b. Termine poco selettivo: il caso peggiore.
-- Serve a due scopi: capire se l'indice di ordinamento viene usato quando le
-- righe da ordinare sono molte, e se il minimo di 3 caratteri imposto dall'API
-- basta a evitare ricerche costose.
--
-- Attenzione a scegliere il termine: nelle denominazioni AIFA le parole sono
-- abbreviate (CPR, non "compresse"), quindi un termine dal linguaggio comune
-- può risultare selettivo e non misurare nulla. Individuare prima i termini
-- davvero frequenti:
--
--   SELECT
--       (SELECT count(*) FROM farmaci_classe_a) AS righe_totali,
--       (SELECT count(*) FROM farmaci_classe_a
--         WHERE denominazione_confezione ILIKE '%cpr%') AS con_cpr,
--       (SELECT count(*) FROM farmaci_classe_a
--         WHERE denominazione_confezione ILIKE '%ina%') AS con_ina;
--
-- poi sostituire il termine qui sotto con quello più frequente.

EXPLAIN ANALYZE
SELECT aic, principio_attivo, denominazione_confezione
FROM farmaci_classe_a
WHERE principio_attivo ILIKE '%cpr%'
   OR denominazione_confezione ILIKE '%cpr%'
   OR descrizione_gruppo ILIKE '%cpr%'
ORDER BY denominazione_confezione, aic
LIMIT 20;

-- Nota sulle stime: il planner prevedeva 2 righe e ne ha trovate 14 nel primo
-- test. Le stime di selettività su ILIKE '%...%' sono strutturalmente
-- approssimative, quindi su termini molto frequenti la scelta del piano può
-- cambiare in modo poco prevedibile. È un'altra ragione per misurare invece di
-- dedurre.

-- ---------------------------------------------------------------------------
-- Conclusione su idx_farmaci_classe_a_denominazione_ordinamento
-- ---------------------------------------------------------------------------
-- Con termini selettivi il planner lo ignora e ordina in memoria. Si potrebbe
-- quindi rimuovere, ma occupa 784 kB su una quota di 500 MB (lo 0,16%) e
-- proteggerebbe il caso peggiore di un termine molto frequente. Il rapporto tra
-- rischio e beneficio non giustifica la rimozione: si tiene.
