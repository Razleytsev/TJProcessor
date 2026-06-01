-- ============================================================
-- R7 reseed for TJProcessor — adds MORE Order Batches and
-- Submission Requests (with child CodeOrders / Packages) so that
-- pagination and column-drift are visible across several pages.
--
-- Page sizes in the UI: Batches=20/page, Submission Requests=20/page,
-- Orders=5/page, Packages=5/page. This brings totals to ~45 batches
-- and ~50 requests => 3 pages each, with 1- and 2-digit ids straddling
-- page boundaries (the TB1/TB3/SR1 drift case).
--
-- Idempotent: tagged rows (Description '[r7]%' / Filename 'r7-%.xml')
-- are skipped on re-run. Matches the live schema, FKs, enums, dates.
--
-- Run inside the docker container:
--   docker exec -i tj-postgres psql -U postgres -d postgres < docs/sql/seed-r7-pagination.sql
-- ============================================================

BEGIN;

-- ── More Order Batches (+ child CodeOrders) ──────────────────
-- Batch.Status enum: -1 Canceled, 0 Created, 1 Processing, 2 Completed.
-- CodeOrder.Status enum (OrderStatusInfo / numeric): 0 Created, 1 Processing,
-- 2-4 in-flight, 5 Done, negatives = error. We spread statuses so the orders
-- sub-table (5/page) shows multiple pages and varied dots.
DO $$
DECLARE
    n              INT;
    prod_id        INT;
    prod_type      INT;
    batch_id       INT;
    b_status       INT;
    n_orders       INT;
    k              INT;
    o_status       INT;
    base_date      TIMESTAMPTZ;
BEGIN
    IF EXISTS (SELECT 1 FROM "Batches" WHERE "Description" LIKE '[r7]%') THEN
        RAISE NOTICE 'R7 batches already present, skipping.';
    ELSE
        -- 40 additional batches => with the existing 6 that is 46 total (3 pages).
        FOR n IN 1..40 LOOP
            -- Pick a product (round-robin over the 24 demo products).
            SELECT "Id", "Type" INTO prod_id, prod_type
            FROM "Products" ORDER BY "Id" OFFSET (n % 24) LIMIT 1;

            -- Mix of statuses, weighted toward Completed so most have orders.
            b_status := CASE (n % 6)
                            WHEN 0 THEN 0     -- Created
                            WHEN 1 THEN 1     -- Processing
                            WHEN 2 THEN -1    -- Canceled
                            ELSE 2            -- Completed
                        END;
            base_date := now() - ((n) || ' days')::interval - interval '1 hour';

            INSERT INTO "Batches" ("Type", "ProductId", "Count", "Status", "Description", "User", "RecordDate", "StatusHistoryJson")
            VALUES (
                prod_type, prod_id, 1000 + (n * 250), b_status,
                '[r7] Batch #' || n || ' ' || (CASE prod_type WHEN 0 THEN 'Pack' WHEN 1 THEN 'Bundle' ELSE 'Mastercase' END),
                (CASE (n % 3) WHEN 0 THEN 'admin' WHEN 1 THEN 'user' ELSE 'opera tor' END),
                base_date,
                CASE b_status
                    WHEN 0 THEN jsonb_build_array(jsonb_build_object('Status',0,'StatusDate',base_date::text))
                    WHEN -1 THEN jsonb_build_array(
                        jsonb_build_object('Status',0,'StatusDate',base_date::text),
                        jsonb_build_object('Status',-1,'StatusDate',(base_date + interval '5 minutes')::text))
                    ELSE jsonb_build_array(
                        jsonb_build_object('Status',0,'StatusDate',base_date::text),
                        jsonb_build_object('Status',1,'StatusDate',(base_date + interval '2 minutes')::text),
                        jsonb_build_object('Status',2,'StatusDate',(base_date + interval '12 minutes')::text))
                END
            )
            RETURNING "Id" INTO batch_id;

            -- Child orders: 6-8 each on Completed batches (>5 => multi-page orders
            -- sub-table); 1-2 on others. Skip Created/Canceled child orders sparingly.
            n_orders := CASE WHEN b_status = 2 THEN 6 + (n % 3)
                             WHEN b_status = 1 THEN 2
                             WHEN b_status = -1 THEN 1
                             ELSE 1 END;

            FOR k IN 1..n_orders LOOP
                o_status := CASE
                                WHEN b_status = 2 THEN 5                          -- Done
                                WHEN b_status = 1 THEN (CASE WHEN k = 1 THEN 3 ELSE 1 END)
                                WHEN b_status = -1 THEN -1                        -- error
                                ELSE 0                                            -- Created
                            END;
                INSERT INTO "CodeOrders" ("Type", "ProductId", "Count", "Status", "ExternalGuid", "Description", "User", "RecordDate",
                                          "StatusHistoryJson", "StatusMessage", "BatchId")
                VALUES (
                    prod_type, prod_id, 500 + (k * 100), o_status,
                    CASE WHEN o_status = 0 THEN NULL ELSE gen_random_uuid() END,
                    '[r7] Order #' || n || '.' || k, (CASE (n % 3) WHEN 0 THEN 'admin' WHEN 1 THEN 'user' ELSE 'opera tor' END),
                    base_date + (k || ' minutes')::interval,
                    CASE o_status
                        WHEN 5 THEN jsonb_build_array(
                            jsonb_build_object('Status',0,'StatusDate',base_date::text),
                            jsonb_build_object('Status',5,'StatusDate',(base_date + interval '11 minutes')::text))
                        WHEN -1 THEN jsonb_build_array(
                            jsonb_build_object('Status',0,'StatusDate',base_date::text),
                            jsonb_build_object('Status',-1,'StatusDate',(base_date + interval '4 minutes')::text))
                        ELSE jsonb_build_array(jsonb_build_object('Status',o_status,'StatusDate',base_date::text))
                    END,
                    CASE WHEN o_status = -1 THEN 'External API returned 422: GTIN not authorised.' ELSE NULL END,
                    batch_id);
            END LOOP;
        END LOOP;
        RAISE NOTICE 'R7 batches + orders inserted.';
    END IF;
END $$;

-- CodeOrdersContents for the R7 Done orders (small slice of codes, like the
-- demo seed) so Download/detail paths have content.
INSERT INTO "CodeOrdersContents" ("Id", "CodeOrderId", "OrderContent", "RecordDate", "DownloadHistory")
SELECT co."Id", co."Id",
       ARRAY(
           SELECT '01' || co."Id"::text || lpad(gs::text, 6, '0') || '21' || substring(md5(random()::text || clock_timestamp()::text) for 12)
           FROM generate_series(1, LEAST(co."Count", 8)) AS gs
       ),
       co."RecordDate" + interval '12 minutes',
       jsonb_build_array(jsonb_build_object('User', 'opera tor', 'Date', (co."RecordDate" + interval '20 minutes')::text))
FROM "CodeOrders" co
WHERE co."Status" = 5
  AND co."Description" LIKE '[r7]%'
  AND NOT EXISTS (SELECT 1 FROM "CodeOrdersContents" cc WHERE cc."Id" = co."Id");

-- ── More Submission Requests (+ child Packages) ──────────────
-- Package.Status enum (PkgStatusInfo): <0 error, 0 Created, 1-3 info,
-- 4-11 processing, 12 Reported (final). Code = 01 + GTIN(14) + 21 + serial(12);
-- SSCC = 00 + 16 digits. Both Code and SSCCCode are UNIQUE.
DO $$
DECLARE
    n          INT;
    req_id     INT;
    n_pkgs     INT;
    k          INT;
    p_status   INT;
    base_date  TIMESTAMPTZ;
    serial     BIGINT;
BEGIN
    IF EXISTS (SELECT 1 FROM "PackageRequests" WHERE "Filename" LIKE 'r7-%.xml') THEN
        RAISE NOTICE 'R7 package requests already present, skipping.';
    ELSE
        -- 30 additional requests => with the existing 20 that is 50 total (3 pages).
        FOR n IN 1..30 LOOP
            base_date := now() - ((n) || ' days')::interval - interval '30 minutes';
            INSERT INTO "PackageRequests" ("Filename", "User", "Status", "RecordDate", "StatusHistory")
            VALUES (
                'r7-submission-' || lpad(n::text, 3, '0') || '.xml',
                (CASE (n % 3) WHEN 0 THEN 'admin' WHEN 1 THEN 'user' ELSE 'opera tor' END),
                0, base_date,
                jsonb_build_object('Status', 0, 'StatusDate', base_date::text))
            RETURNING "Id" INTO req_id;

            -- 7-11 packages each (>5 => multi-page packages sub-table). Includes a
            -- few error-status packages so the optional Retry button (SR2) appears.
            n_pkgs := 7 + (n % 5);
            FOR k IN 1..n_pkgs LOOP
                -- Spread statuses: mostly Reported, some processing, some error.
                p_status := CASE
                                WHEN k % 7 = 0 THEN -8     -- error -> Retry button shows
                                WHEN k % 7 = 1 THEN 5      -- processing
                                WHEN k % 7 = 2 THEN 9      -- processing
                                WHEN k % 7 = 3 THEN 0      -- created
                                ELSE 12                    -- Reported (final)
                            END;
                -- Unique-ish serial namespace: (10000 + n)*1000 + k keeps Code/SSCC unique
                -- across all R7 rows and clear of the existing demo serials.
                serial := (10000 + n)::bigint * 1000 + k;
                INSERT INTO "Packages" ("Code", "SSCCCode", "Status", "RecordDate", "PackageRequestId", "StatusHistory",
                                        "ContentApplicationGuid", "AggregationGuid", "Comment")
                VALUES (
                    '0104601234567890' || '21' || lpad(serial::text, 12, '0'),
                    '00' || lpad(serial::text, 18, '0'),
                    p_status,
                    base_date + (k || ' minutes')::interval,
                    req_id,
                    CASE p_status
                        WHEN 12 THEN jsonb_build_array(
                            jsonb_build_object('Status',0,'StatusDate',base_date::text),
                            jsonb_build_object('Status',3,'StatusDate',(base_date + interval '5 minutes')::text),
                            jsonb_build_object('Status',9,'StatusDate',(base_date + interval '9 minutes')::text),
                            jsonb_build_object('Status',12,'StatusDate',(base_date + interval '15 minutes')::text))
                        WHEN -8 THEN jsonb_build_array(
                            jsonb_build_object('Status',0,'StatusDate',base_date::text),
                            jsonb_build_object('Status',4,'StatusDate',(base_date + interval '4 minutes')::text),
                            jsonb_build_object('Status',-8,'StatusDate',(base_date + interval '8 minutes')::text))
                        WHEN 0 THEN jsonb_build_array(jsonb_build_object('Status',0,'StatusDate',base_date::text))
                        ELSE jsonb_build_array(
                            jsonb_build_object('Status',0,'StatusDate',base_date::text),
                            jsonb_build_object('Status',p_status,'StatusDate',(base_date + interval '6 minutes')::text))
                    END,
                    CASE WHEN p_status >= 4 THEN gen_random_uuid() ELSE NULL END,
                    CASE WHEN p_status >= 9 THEN gen_random_uuid() ELSE NULL END,
                    CASE WHEN p_status = -8 THEN 'External API rejected the aggregation payload — HTTP 502.' ELSE NULL END);
            END LOOP;
        END LOOP;
        RAISE NOTICE 'R7 requests + packages inserted.';
    END IF;
END $$;

COMMIT;

-- Row counts after seeding
SELECT
    (SELECT count(*) FROM "Batches")                                      AS batches_total,
    (SELECT count(*) FROM "Batches" WHERE "Description" LIKE '[r7]%')     AS r7_batches,
    (SELECT count(*) FROM "CodeOrders" WHERE "Description" LIKE '[r7]%')  AS r7_orders,
    (SELECT count(*) FROM "PackageRequests")                             AS requests_total,
    (SELECT count(*) FROM "PackageRequests" WHERE "Filename" LIKE 'r7-%.xml') AS r7_requests,
    (SELECT count(*) FROM "Packages")                                    AS packages_total;
