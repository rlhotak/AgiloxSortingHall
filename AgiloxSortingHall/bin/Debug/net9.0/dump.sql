BEGIN TRANSACTION;
CREATE TABLE IF NOT EXISTS "HallRows" (
	"Id"	INTEGER NOT NULL,
	"Name"	TEXT NOT NULL,
	"ColorHex"	TEXT NOT NULL,
	"Capacity"	INTEGER NOT NULL,
	"Article"	TEXT NOT NULL,
	CONSTRAINT "PK_HallRows" PRIMARY KEY("Id" AUTOINCREMENT)
);
CREATE TABLE IF NOT EXISTS "HallSettings" (
	"Id"	INTEGER NOT NULL,
	"RowSelectionStrategy"	INTEGER NOT NULL,
	"PickupStationAreaName"	TEXT NOT NULL DEFAULT '',
	"DropRowSelectionStrategy"	INTEGER NOT NULL DEFAULT 0,
	"DropStationAreaName"	TEXT NOT NULL DEFAULT '',
	CONSTRAINT "PK_HallSettings" PRIMARY KEY("Id" AUTOINCREMENT)
);
CREATE TABLE IF NOT EXISTS "PalletSlots" (
	"Id"	INTEGER NOT NULL,
	"HallRowId"	INTEGER NOT NULL,
	"PositionIndex"	INTEGER NOT NULL,
	"State"	INTEGER NOT NULL,
	CONSTRAINT "PK_PalletSlots" PRIMARY KEY("Id" AUTOINCREMENT),
	CONSTRAINT "FK_PalletSlots_HallRows_HallRowId" FOREIGN KEY("HallRowId") REFERENCES "HallRows"("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "RowCalls" (
	"Id"	INTEGER NOT NULL,
	"WorkTableId"	INTEGER NOT NULL,
	"HallRowId"	INTEGER,
	"RequestedAt"	TEXT NOT NULL,
	"Status"	INTEGER NOT NULL,
	"OrderId"	INTEGER,
	"LastAgiloxStatus"	TEXT,
	"LastAgiloxAction"	TEXT,
	"PickedSlotId"	INTEGER,
	CONSTRAINT "PK_RowCalls" PRIMARY KEY("Id" AUTOINCREMENT),
	CONSTRAINT "FK_RowCalls_HallRows_HallRowId" FOREIGN KEY("HallRowId") REFERENCES "HallRows"("Id"),
	CONSTRAINT "FK_RowCalls_WorkTables_WorkTableId" FOREIGN KEY("WorkTableId") REFERENCES "WorkTables"("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "WorkTables" (
	"Id"	INTEGER NOT NULL,
	"DisplayName"	TEXT,
	"InputStationName"	TEXT NOT NULL,
	"OutputStationName"	TEXT NOT NULL,
	"Category"	INTEGER NOT NULL,
	CONSTRAINT "PK_WorkTables" PRIMARY KEY("Id" AUTOINCREMENT)
);
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
	"MigrationId"	TEXT NOT NULL,
	"ProductVersion"	TEXT NOT NULL,
	CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY("MigrationId")
);
CREATE TABLE IF NOT EXISTS "__EFMigrationsLock" (
	"Id"	INTEGER NOT NULL,
	"Timestamp"	TEXT NOT NULL,
	CONSTRAINT "PK___EFMigrationsLock" PRIMARY KEY("Id")
);
INSERT INTO "HallRows" VALUES (1,'Řada1','#F4A261',3,'14393');
INSERT INTO "HallRows" VALUES (2,'Řada2','#2A9D8F',3,'14393');
INSERT INTO "HallRows" VALUES (3,'Řada3','#457B9D',3,'14393');
INSERT INTO "HallRows" VALUES (4,'Řada4','#E76F51',3,'14547');
INSERT INTO "HallRows" VALUES (6,'Řada5','#0d6efd',3,'14547');
INSERT INTO "HallSettings" VALUES (1,0,'Buffer',0,'Hotovo');
INSERT INTO "PalletSlots" VALUES (1,1,0,1);
INSERT INTO "PalletSlots" VALUES (2,1,1,1);
INSERT INTO "PalletSlots" VALUES (3,1,2,1);
INSERT INTO "PalletSlots" VALUES (4,2,0,1);
INSERT INTO "PalletSlots" VALUES (5,2,1,1);
INSERT INTO "PalletSlots" VALUES (6,2,2,1);
INSERT INTO "PalletSlots" VALUES (7,3,0,1);
INSERT INTO "PalletSlots" VALUES (8,3,1,1);
INSERT INTO "PalletSlots" VALUES (9,3,2,1);
INSERT INTO "PalletSlots" VALUES (10,4,0,0);
INSERT INTO "PalletSlots" VALUES (11,4,1,0);
INSERT INTO "PalletSlots" VALUES (12,4,2,0);
INSERT INTO "PalletSlots" VALUES (16,6,0,1);
INSERT INTO "PalletSlots" VALUES (17,6,1,1);
INSERT INTO "PalletSlots" VALUES (18,6,2,1);
INSERT INTO "WorkTables" VALUES (1,'Stůl 1','p2s1_vstup','p2s1_vystup',3);
INSERT INTO "WorkTables" VALUES (2,'Stůl 2','p2s2_vstup','p2s2_vystup',3);
INSERT INTO "WorkTables" VALUES (3,'Stůl 3','p2s3_vstup','p2s3_vystup',3);
INSERT INTO "WorkTables" VALUES (4,'Kontrola 1','Kontrola 1','Kontrola 1',1);
INSERT INTO "WorkTables" VALUES (5,'Kontrola 2','Kontrola 2','Kontrola 2',1);
INSERT INTO "WorkTables" VALUES (6,'Kontrola 3','Kontrola 3','Kontrola 3',1);
INSERT INTO "WorkTables" VALUES (7,'Kontrola 4','Kontrola 4','Kontrola 4',1);
INSERT INTO "WorkTables" VALUES (8,'Kontrola 5','Kontrola 5','Kontrola 5',1);
INSERT INTO "WorkTables" VALUES (9,'Stůl 1','p7s1_vstup','p7s1_vystup',8);
INSERT INTO "WorkTables" VALUES (10,'Stůl 2','p7s2_vstup','p7s2_vystup',8);
INSERT INTO "WorkTables" VALUES (11,'Stůl 3','p7s3_vstup','p7s3_vystup',8);
INSERT INTO "WorkTables" VALUES (12,'Stůl 1','p3s1_vstup','p3s1_vystup',4);
INSERT INTO "WorkTables" VALUES (13,'Stůl 2','p3s2_vstup','p3s2_vystup',4);
INSERT INTO "WorkTables" VALUES (14,'Stůl 3','p3s3_vstup','p3s3_vystup',4);
INSERT INTO "WorkTables" VALUES (15,'Stůl 1','p6s1_vstup','p6s1_vystup',7);
INSERT INTO "WorkTables" VALUES (16,'Stůl 2','p6s2_vstup','p6s2_vystup',7);
INSERT INTO "WorkTables" VALUES (17,'Stůl 3','p6s3_vstup','p6s3_vystup',7);
INSERT INTO "WorkTables" VALUES (18,'Stůl 1','p4s1_vstup','p4s1_vystup',5);
INSERT INTO "WorkTables" VALUES (19,'Stůl 2','p4s2_vstup','p4s2_vystup',5);
INSERT INTO "WorkTables" VALUES (20,'Stůl 3','p4s3_vstup','p4s3_vystup',5);
INSERT INTO "WorkTables" VALUES (21,'Stůl 1','p5s1_vstup','p5s1_vystup',6);
INSERT INTO "WorkTables" VALUES (22,'Stůl 2','p5s2_vstup','p5s2_vystup',6);
INSERT INTO "WorkTables" VALUES (23,'Stůl 3','p5s3_vstup','p5s3_vystup',6);
INSERT INTO "WorkTables" VALUES (24,'Kontrola 6','Kontrola 6','Kontrola 6',1);
INSERT INTO "WorkTables" VALUES (25,'Kontrola 7','Kontrola 7','Kontrola 7',1);
INSERT INTO "__EFMigrationsHistory" VALUES ('20260105171319_Initial','9.0.11');
INSERT INTO "__EFMigrationsHistory" VALUES ('20260127091643_AddStationAreaNameToHallSettings','9.0.11');
INSERT INTO "__EFMigrationsHistory" VALUES ('20260414082630_AddPutRowStrategy','9.0.11');
INSERT INTO "__EFMigrationsHistory" VALUES ('20260415070155_SplitStationAreaNameIntoPickupAndDrop','9.0.11');
INSERT INTO "__EFMigrationsHistory" VALUES ('20260605080000_AddPickedSlotToRowCall','9.0.11');
CREATE UNIQUE INDEX IF NOT EXISTS "IX_PalletSlots_HallRowId_PositionIndex" ON "PalletSlots" (
	"HallRowId",
	"PositionIndex"
);
CREATE INDEX IF NOT EXISTS "IX_RowCalls_HallRowId" ON "RowCalls" (
	"HallRowId"
);
CREATE INDEX IF NOT EXISTS "IX_RowCalls_PickedSlotId" ON "RowCalls" (
	"PickedSlotId"
);
CREATE INDEX IF NOT EXISTS "IX_RowCalls_WorkTableId" ON "RowCalls" (
	"WorkTableId"
);
COMMIT;
