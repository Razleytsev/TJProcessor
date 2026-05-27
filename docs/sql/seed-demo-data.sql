-- ============================================================
-- Demo seed for TJProcessor — populates Products, Batches,
-- CodeOrders, PackageRequests, and Packages with a realistic mix
-- of statuses so every UI panel has something to render.
--
-- Idempotent: skips inserts when the GTIN / Filename already exists.
-- Run inside the docker container:
--   docker exec -i tj-postgres psql -U postgres -d postgres < docs/sql/seed-demo-data.sql
-- ============================================================

BEGIN;

-- ── Reference data (Factory / MarkingLine / Location) ────────
-- The API's startup pre-creates one of each. We're not adding more;
-- form auto-selects the only-one when count == 1.

-- ── Products ────────────────────────────────────────────────
-- Types: 0 = Pack, 1 = Bundle, 3 = Mastercase
INSERT INTO "Products" ("Type", "Gtin", "Name", "ExternalUid", "RecordDate")
SELECT * FROM (VALUES
    (0, '04601234567890', 'Crystal Water 0.5L Pack',                gen_random_uuid(), now() - interval '40 days'),
    (0, '04601234567906', 'Crystal Water 1.5L Pack',                gen_random_uuid(), now() - interval '38 days'),
    (0, '04601234567913', 'Sparkling Lemon 0.5L Pack',              gen_random_uuid(), now() - interval '35 days'),
    (0, '04601234567920', 'Tea Mountain Black 0.5L Pack',           gen_random_uuid(), now() - interval '30 days'),
    (0, '04601234567937', 'Tea Mountain Green 0.5L Pack',           gen_random_uuid(), now() - interval '28 days'),
    (1, '04601234567944', 'Crystal Water 0.5L Bundle (12)',         gen_random_uuid(), now() - interval '40 days'),
    (1, '04601234567951', 'Crystal Water 1.5L Bundle (6)',          gen_random_uuid(), now() - interval '38 days'),
    (1, '04601234567968', 'Sparkling Lemon 0.5L Bundle (12)',       gen_random_uuid(), now() - interval '35 days'),
    (1, '04601234567975', 'Tea Mountain Mixed Bundle (12)',         gen_random_uuid(), now() - interval '30 days'),
    (3, '04601234567982', 'Mastercase Generic',                     gen_random_uuid(), now() - interval '45 days')
) AS v ("Type", "Gtin", "Name", "ExternalUid", "RecordDate")
WHERE NOT EXISTS (SELECT 1 FROM "Products" WHERE "Gtin" = v."Gtin");

-- ── Batches ─────────────────────────────────────────────────
-- Status: -1 Canceled, 0 Created, 1 Processing, 2 Completed
DO $$
DECLARE
    pack_a   INT;
    pack_b   INT;
    bundle_a INT;
    mc_id    INT;
    b1 INT; b2 INT; b3 INT; b4 INT; b5 INT; b6 INT;
BEGIN
    -- Skip if we already seeded batches (idempotency guard)
    IF EXISTS (SELECT 1 FROM "Batches" WHERE "Description" LIKE '[demo]%') THEN
        RAISE NOTICE 'Demo batches already present, skipping.';
        RETURN;
    END IF;

    SELECT "Id" INTO pack_a   FROM "Products" WHERE "Gtin" = '04601234567890';
    SELECT "Id" INTO pack_b   FROM "Products" WHERE "Gtin" = '04601234567906';
    SELECT "Id" INTO bundle_a FROM "Products" WHERE "Gtin" = '04601234567944';
    SELECT "Id" INTO mc_id    FROM "Products" WHERE "Gtin" = '04601234567982';

    -- B1: Completed pack batch — 500 codes (the "happy path" sample)
    INSERT INTO "Batches" ("Type", "ProductId", "Count", "Status", "Description", "User", "RecordDate", "StatusHistoryJson")
    VALUES (0, pack_a, 500, 2, '[demo] Crystal 0.5L initial mint', 'opera tor', now() - interval '14 days',
            jsonb_build_array(
                jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '14 days')::text),
                jsonb_build_object('Status', 1, 'StatusDate', (now() - interval '14 days' + interval '2 minutes')::text),
                jsonb_build_object('Status', 2, 'StatusDate', (now() - interval '14 days' + interval '11 minutes')::text)
            ))
    RETURNING "Id" INTO b1;

    -- B2: Completed pack batch — 1000 codes, different product
    INSERT INTO "Batches" ("Type", "ProductId", "Count", "Status", "Description", "User", "RecordDate", "StatusHistoryJson")
    VALUES (0, pack_b, 1000, 2, '[demo] Crystal 1.5L mint', 'opera tor', now() - interval '9 days',
            jsonb_build_array(
                jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '9 days')::text),
                jsonb_build_object('Status', 1, 'StatusDate', (now() - interval '9 days' + interval '1 minute')::text),
                jsonb_build_object('Status', 2, 'StatusDate', (now() - interval '9 days' + interval '14 minutes')::text)
            ))
    RETURNING "Id" INTO b2;

    -- B3: Bundle batch — completed, 240 codes
    INSERT INTO "Batches" ("Type", "ProductId", "Count", "Status", "Description", "User", "RecordDate", "StatusHistoryJson")
    VALUES (1, bundle_a, 240, 2, '[demo] Crystal 0.5L Bundle (12)', 'opera tor', now() - interval '6 days',
            jsonb_build_array(
                jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '6 days')::text),
                jsonb_build_object('Status', 1, 'StatusDate', (now() - interval '6 days' + interval '1 minute')::text),
                jsonb_build_object('Status', 2, 'StatusDate', (now() - interval '6 days' + interval '8 minutes')::text)
            ))
    RETURNING "Id" INTO b3;

    -- B4: In-flight pack batch — Processing (Status 1)
    INSERT INTO "Batches" ("Type", "ProductId", "Count", "Status", "Description", "User", "RecordDate", "StatusHistoryJson")
    VALUES (0, pack_a, 2000, 1, '[demo] Crystal 0.5L scale-up', 'shift manager', now() - interval '2 hours',
            jsonb_build_array(
                jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '2 hours')::text),
                jsonb_build_object('Status', 1, 'StatusDate', (now() - interval '2 hours' + interval '40 seconds')::text)
            ))
    RETURNING "Id" INTO b4;

    -- B5: Canceled pack batch
    INSERT INTO "Batches" ("Type", "ProductId", "Count", "Status", "Description", "User", "RecordDate", "StatusHistoryJson")
    VALUES (0, pack_b, 300, -1, '[demo] Crystal 1.5L scrapped run', 'shift manager', now() - interval '3 days',
            jsonb_build_array(
                jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '3 days')::text),
                jsonb_build_object('Status', 1, 'StatusDate', (now() - interval '3 days' + interval '1 minute')::text),
                jsonb_build_object('Status', -1, 'StatusDate', (now() - interval '3 days' + interval '4 minutes')::text)
            ))
    RETURNING "Id" INTO b5;

    -- B6: Mastercase batch — Created (waiting to submit)
    INSERT INTO "Batches" ("Type", "ProductId", "Count", "Status", "Description", "User", "RecordDate", "StatusHistoryJson")
    VALUES (3, mc_id, 20, 0, '[demo] Mastercase batch queued', 'opera tor', now() - interval '30 minutes',
            jsonb_build_array(
                jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '30 minutes')::text)
            ))
    RETURNING "Id" INTO b6;

    -- ── CodeOrders attached to those batches ───────────────
    -- B1 Completed → one order, Done
    INSERT INTO "CodeOrders" ("Type", "ProductId", "Count", "Status", "ExternalGuid", "Description", "User", "RecordDate",
                              "StatusHistoryJson", "StatusMessage", "BatchId")
    VALUES (0, pack_a, 500, 5, gen_random_uuid(),
            '[demo] Crystal 0.5L initial mint', 'opera tor', now() - interval '14 days',
            jsonb_build_array(
                jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '14 days')::text),
                jsonb_build_object('Status', 1, 'StatusDate', (now() - interval '14 days' + interval '2 minutes')::text),
                jsonb_build_object('Status', 4, 'StatusDate', (now() - interval '14 days' + interval '9 minutes')::text),
                jsonb_build_object('Status', 5, 'StatusDate', (now() - interval '14 days' + interval '11 minutes')::text)
            ),
            'Codes delivered.', b1);

    -- B2 Completed → split into 2 orders of 500
    INSERT INTO "CodeOrders" ("Type", "ProductId", "Count", "Status", "ExternalGuid", "Description", "User", "RecordDate",
                              "StatusHistoryJson", "BatchId")
    VALUES
    (0, pack_b, 500, 5, gen_random_uuid(), '[demo] Crystal 1.5L mint #1', 'opera tor', now() - interval '9 days',
        jsonb_build_array(
            jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '9 days')::text),
            jsonb_build_object('Status', 5, 'StatusDate', (now() - interval '9 days' + interval '13 minutes')::text)
        ), b2),
    (0, pack_b, 500, 5, gen_random_uuid(), '[demo] Crystal 1.5L mint #2', 'opera tor', now() - interval '9 days',
        jsonb_build_array(
            jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '9 days')::text),
            jsonb_build_object('Status', 5, 'StatusDate', (now() - interval '9 days' + interval '14 minutes')::text)
        ), b2);

    -- B3 Bundle Completed → one order, Done
    INSERT INTO "CodeOrders" ("Type", "ProductId", "Count", "Status", "ExternalGuid", "Description", "User", "RecordDate",
                              "StatusHistoryJson", "BatchId")
    VALUES (1, bundle_a, 240, 5, gen_random_uuid(),
            '[demo] Bundle order', 'opera tor', now() - interval '6 days',
            jsonb_build_array(
                jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '6 days')::text),
                jsonb_build_object('Status', 5, 'StatusDate', (now() - interval '6 days' + interval '7 minutes')::text)
            ), b3);

    -- B4 Processing → 2 orders mid-flight (Sent / Executing)
    INSERT INTO "CodeOrders" ("Type", "ProductId", "Count", "Status", "ExternalGuid", "Description", "User", "RecordDate",
                              "StatusHistoryJson", "BatchId")
    VALUES
    (0, pack_a, 1000, 3, gen_random_uuid(), '[demo] Scale-up order #1', 'shift manager', now() - interval '2 hours',
        jsonb_build_array(
            jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '2 hours')::text),
            jsonb_build_object('Status', 1, 'StatusDate', (now() - interval '2 hours' + interval '30 seconds')::text),
            jsonb_build_object('Status', 3, 'StatusDate', (now() - interval '90 minutes')::text)
        ), b4),
    (0, pack_a, 1000, 1, gen_random_uuid(), '[demo] Scale-up order #2', 'shift manager', now() - interval '1 hour 50 minutes',
        jsonb_build_array(
            jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '1 hour 50 minutes')::text),
            jsonb_build_object('Status', 1, 'StatusDate', (now() - interval '1 hour 49 minutes')::text)
        ), b4);

    -- B5 Canceled → one order in error state
    INSERT INTO "CodeOrders" ("Type", "ProductId", "Count", "Status", "ExternalGuid", "Description", "User", "RecordDate",
                              "StatusHistoryJson", "StatusMessage", "BatchId")
    VALUES (0, pack_b, 300, -1, gen_random_uuid(),
            '[demo] Scrapped order', 'shift manager', now() - interval '3 days',
            jsonb_build_array(
                jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '3 days')::text),
                jsonb_build_object('Status', 1, 'StatusDate', (now() - interval '3 days' + interval '1 minute')::text),
                jsonb_build_object('Status', -1, 'StatusDate', (now() - interval '3 days' + interval '4 minutes')::text)
            ),
            'External API returned 422: GTIN not authorised for this market.', b5);

    -- B6 Created Mastercase → one queued order
    INSERT INTO "CodeOrders" ("Type", "ProductId", "Count", "Status", "Description", "User", "RecordDate",
                              "StatusHistoryJson", "BatchId")
    VALUES (3, mc_id, 20, 0,
            '[demo] Mastercase queued', 'opera tor', now() - interval '30 minutes',
            jsonb_build_array(
                jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '30 minutes')::text)
            ), b6);

    -- ── CodeOrderContents for finished orders (a small slice of codes) ─
    INSERT INTO "CodeOrdersContents" ("Id", "CodeOrderId", "OrderContent", "RecordDate", "DownloadHistory")
    SELECT co."Id", co."Id",
           ARRAY(
               SELECT '01' || co."Id"::text || lpad(gs::text, 6, '0') || '21' || substring(md5(random()::text || clock_timestamp()::text) for 12)
               FROM generate_series(1, LEAST(co."Count", 8)) AS gs
           ),
           co."RecordDate" + interval '12 minutes',
           jsonb_build_array(
               jsonb_build_object('User', 'opera tor', 'Date', (co."RecordDate" + interval '20 minutes')::text)
           )
    FROM "CodeOrders" co
    WHERE co."Status" = 5
      AND co."Description" LIKE '[demo]%'
      AND NOT EXISTS (SELECT 1 FROM "CodeOrdersContents" cc WHERE cc."Id" = co."Id");

END $$;

-- ── PackageRequests + Packages ──────────────────────────────
-- Status codes for Packages (from Containers.razor PkgStatusInfo):
--   < 0: error states, 0: Created, 1-3: info,
--   4-11: processing, 12: Reported (final)
DO $$
DECLARE
    pr1 INT; pr2 INT; pr3 INT;
BEGIN
    IF EXISTS (SELECT 1 FROM "PackageRequests" WHERE "Filename" LIKE 'demo-%.csv') THEN
        RAISE NOTICE 'Demo package requests already present, skipping.';
        RETURN;
    END IF;

    -- PR1: Older request, all 5 packages "Reported" (Status 12)
    INSERT INTO "PackageRequests" ("Filename", "User", "Status", "RecordDate", "StatusHistory")
    VALUES ('demo-mc-feb01.csv', 'opera tor', 0, now() - interval '20 days',
            jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '20 days')::text))
    RETURNING "Id" INTO pr1;

    INSERT INTO "Packages" ("Code", "SSCCCode", "Status", "RecordDate", "PackageRequestId", "StatusHistory")
    SELECT
        '01' || lpad((pr1 * 100 + gs)::text, 14, '0') || '21' || substring(md5(random()::text || clock_timestamp()::text) for 12),
        '00' || lpad((pr1 * 1000 + gs)::text, 16, '0'),
        12,
        now() - interval '20 days' + (gs || ' minutes')::interval,
        pr1,
        jsonb_build_array(
            jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '20 days')::text),
            jsonb_build_object('Status', 12, 'StatusDate', (now() - interval '20 days' + interval '15 minutes')::text)
        )
    FROM generate_series(1, 5) AS gs;

    -- PR2: Recent request, mixed statuses (one in-flight, one error, one done)
    INSERT INTO "PackageRequests" ("Filename", "User", "Status", "RecordDate", "StatusHistory")
    VALUES ('demo-mc-yesterday.csv', 'opera tor', 0, now() - interval '20 hours',
            jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '20 hours')::text))
    RETURNING "Id" INTO pr2;

    INSERT INTO "Packages" ("Code", "SSCCCode", "Status", "RecordDate", "PackageRequestId", "StatusHistory", "Comment")
    VALUES
    ('010460' || lpad((pr2 * 100 + 1)::text, 12, '0') || '21' || substring(md5(random()::text || clock_timestamp()::text) for 12),
     '00' || lpad((pr2 * 1000 + 1)::text, 16, '0'),
     12,
     now() - interval '20 hours',
     pr2,
     jsonb_build_array(
        jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '20 hours')::text),
        jsonb_build_object('Status', 12, 'StatusDate', (now() - interval '19 hours')::text)
     ),
     'OK'),
    ('010460' || lpad((pr2 * 100 + 2)::text, 12, '0') || '21' || substring(md5(random()::text || clock_timestamp()::text) for 12),
     '00' || lpad((pr2 * 1000 + 2)::text, 16, '0'),
     5,
     now() - interval '20 hours',
     pr2,
     jsonb_build_array(
        jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '20 hours')::text),
        jsonb_build_object('Status', 4, 'StatusDate', (now() - interval '19 hours')::text),
        jsonb_build_object('Status', 5, 'StatusDate', (now() - interval '18 hours')::text)
     ),
     'Awaiting external aggregation status.'),
    ('010460' || lpad((pr2 * 100 + 3)::text, 12, '0') || '21' || substring(md5(random()::text || clock_timestamp()::text) for 12),
     '00' || lpad((pr2 * 1000 + 3)::text, 16, '0'),
     -8,
     now() - interval '20 hours',
     pr2,
     jsonb_build_array(
        jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '20 hours')::text),
        jsonb_build_object('Status', 4, 'StatusDate', (now() - interval '19 hours')::text),
        jsonb_build_object('Status', -8, 'StatusDate', (now() - interval '17 hours')::text)
     ),
     'External API rejected the aggregation payload — HTTP 502.');

    -- PR3: Brand new request, everything still "Created"
    INSERT INTO "PackageRequests" ("Filename", "User", "Status", "RecordDate", "StatusHistory")
    VALUES ('demo-mc-fresh.csv', 'shift manager', 0, now() - interval '8 minutes',
            jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '8 minutes')::text))
    RETURNING "Id" INTO pr3;

    INSERT INTO "Packages" ("Code", "SSCCCode", "Status", "RecordDate", "PackageRequestId", "StatusHistory")
    SELECT
        '010460' || lpad((pr3 * 100 + gs)::text, 12, '0') || '21' || substring(md5(random()::text || clock_timestamp()::text) for 12),
        '00' || lpad((pr3 * 1000 + gs)::text, 16, '0'),
        0,
        now() - interval '8 minutes',
        pr3,
        jsonb_build_array(jsonb_build_object('Status', 0, 'StatusDate', (now() - interval '8 minutes')::text))
    FROM generate_series(1, 4) AS gs;
END $$;

COMMIT;

-- Quick check: number of rows seeded
SELECT
    (SELECT count(*) FROM "Products" WHERE "Gtin" LIKE '04601%') AS demo_products,
    (SELECT count(*) FROM "Batches" WHERE "Description" LIKE '[demo]%') AS demo_batches,
    (SELECT count(*) FROM "CodeOrders" WHERE "Description" LIKE '[demo]%') AS demo_orders,
    (SELECT count(*) FROM "CodeOrdersContents") AS code_contents,
    (SELECT count(*) FROM "PackageRequests" WHERE "Filename" LIKE 'demo-%') AS demo_requests,
    (SELECT count(*) FROM "Packages") AS packages;
