CREATE DATABASE Medietica;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS utenti (
	ute_codice			INTEGER NOT NULL DEFAULT 1,
	ute_rag_soc1		VARCHAR (50) NOT NULL DEFAULT '',
	ute_rag_soc2		VARCHAR (50) NOT NULL DEFAULT '',
	ute_desc			VARCHAR (101) NOT NULL DEFAULT '',
	ute_indirizzo		VARCHAR (100) NOT NULL DEFAULT '',
	ute_cap				VARCHAR (5) NOT NULL DEFAULT '',
	ute_prov			VARCHAR (2) NOT NULL DEFAULT '',
	ute_citta			VARCHAR (50) NOT NULL DEFAULT '',
	ute_email			TEXT NOT NULL,
	ute_tel				VARCHAR (15) NOT NULL DEFAULT '',
	ute_cel				VARCHAR (15) NOT NULL DEFAULT '',
	ute_created_at		TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	ute_last_update		TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	ute_password		TEXT NOT NULL,
	ute_level			INTEGER NOT NULL DEFAULT 0,
	ute_type			INTEGER NOT NULL DEFAULT 0,
	CONSTRAINT ute_codice PRIMARY KEY (ute_codice)
);

CREATE INDEX IF NOT EXISTS ute_desc_idx ON utenti (ute_desc);
CREATE UNIQUE INDEX IF NOT EXISTS ute_email_idx ON utenti (ute_email, ute_password);

CREATE OR REPLACE FUNCTION ute_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.ute_rag_soc1	= TRIM(NEW.ute_rag_soc1);
		NEW.ute_rag_soc2	= TRIM(NEW.ute_rag_soc2);
		NEW.ute_desc		= TRIM(CONCAT(NEW.ute_rag_soc1, ' ', NEW.ute_rag_soc2));
		NEW.ute_cap			= TRIM(NEW.ute_cap);
		NEW.ute_prov		= TRIM(NEW.ute_prov);
		NEW.ute_tel			= TRIM(NEW.ute_tel);
		NEW.ute_cel			= TRIM(NEW.ute_cel);
		NEW.ute_email		= LOWER(TRIM(NEW.ute_email));
		NEW.ute_indirizzo	= TRIM(NEW.ute_indirizzo);
		NEW.ute_created_at	= NOW();
		NEW.ute_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.ute_created_at	= NOW();
           NEW.ute_password = CRYPT(TRIM(new.ute_password), GEN_SALT('bf'));
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'ute_trigger') THEN
			CREATE TRIGGER ute_trigger
			BEFORE INSERT OR UPDATE ON utenti
			FOR EACH ROW
			EXECUTE PROCEDURE ute_trigger_function();
		END IF;
	END
$$;

INSERT INTO utenti (ute_codice, ute_rag_soc1, ute_rag_soc2, ute_email, ute_password,  ute_level )
VALUES (1, 'CAPIZZI', 'FILIPPO', 'capizzi@rsaweb.com', 'filippo', 4);

INSERT INTO utenti (ute_codice, ute_rag_soc1, ute_rag_soc2, ute_email, ute_password, ute_level )
VALUES (2, 'SILVIO', 'RICCIUTELLI', 'silvio.ricci.rsa@gmail.com',  'silvio', 4);

INSERT INTO utenti (ute_codice, ute_rag_soc1, ute_rag_soc2, ute_email, ute_password, ute_level )
VALUES (3, 'MANNO', 'ANDREA', 'andream.rsaweb@gmail.com',  'andrea', 4);

/*
	Tabella Immagini Utenti
*/
CREATE TABLE IF NOT EXISTS imgutenti
(
	img_dit			INTEGER NOT NULL DEFAULT 0,
	img_codice		INTEGER NOT NULL CHECK(img_codice > 0),
	img_formato		SMALLINT NOT NULL DEFAULT 0,
	img_tipo		SMALLINT NOT NULL DEFAULT 0,
	img_bytes_size	INTEGER NOT NULL DEFAULT 0,
	img_created_at	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_last_update	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_data		BYTEA,
	CONSTRAINT imu_codice PRIMARY KEY (img_dit, img_codice, img_formato)
);

CREATE OR REPLACE FUNCTION imu_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.img_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.img_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'imu_trigger') THEN
			CREATE TRIGGER imu_trigger
			BEFORE INSERT OR UPDATE ON imgutenti
			FOR EACH ROW
			EXECUTE PROCEDURE imu_trigger_function();
		END IF;
	END
$$;


/*
*	Creazione Tabella UserGrops
*/
CREATE TABLE IF NOT EXISTS usergroups
(
	usg_codice 		    INTEGER NOT NULL DEFAULT 1,
	usg_desc			VARCHAR (50) NOT NULL DEFAULT '',
	usg_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	usg_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT usg_codice PRIMARY KEY (usg_codice)
);
CREATE INDEX usg_desc ON usergroups (usg_desc, usg_codice);

CREATE OR REPLACE FUNCTION usg_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.usg_desc		= TRIM(NEW.usg_desc);
		NEW.usg_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.usg_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'usg_trigger') THEN
			CREATE TRIGGER usg_trigger
			BEFORE INSERT OR UPDATE ON usergroups
			FOR EACH ROW
			EXECUTE PROCEDURE usg_trigger_function();
		END IF;
	END
$$;

CREATE OR REPLACE FUNCTION usg_after_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		INSERT INTO permessi (per_usg, per_end)
 		SELECT usg_codice, end_codice FROM usergroups
    	JOIN endpoints ON end_codice > 0
		ORDER BY usg_codice, end_codice
		ON CONFLICT ON CONSTRAINT per_codice DO NOTHING;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'udg_after_trigger') THEN
			CREATE TRIGGER usg_after_trigger
			AFTER INSERT OR UPDATE ON usergroups
			FOR EACH ROW
			EXECUTE PROCEDURE usg_after_trigger_function();
		END IF;
	END
$$;


INSERT INTO usergroups (usg_codice, usg_desc )
VALUES (1, 'Amministrazione');

INSERT INTO usergroups (usg_codice, usg_desc )
VALUES (2, 'Commerciali');

INSERT INTO usergroups (usg_codice, usg_desc )
VALUES (3, 'Gestori');


/*
*	Creazione Tabella Endpoints
*/
CREATE TABLE IF NOT EXISTS endpoints
(
	end_codice 		    INTEGER NOT NULL DEFAULT 1,
	end_desc			VARCHAR (50) NOT NULL DEFAULT '',
	end_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	end_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT end_codice PRIMARY KEY (end_codice)
);
CREATE INDEX end_desc ON endpoints (end_desc, end_codice);

CREATE OR REPLACE FUNCTION end_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.end_desc		= TRIM(NEW.end_desc);
		NEW.end_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.end_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'end_trigger') THEN
			CREATE TRIGGER end_trigger
			BEFORE INSERT OR UPDATE ON endpoints
			FOR EACH ROW
			EXECUTE PROCEDURE end_trigger_function();
		END IF;
	END
$$;

CREATE OR REPLACE FUNCTION end_after_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		INSERT INTO permessi (per_usg, per_end)
 		SELECT usg_codice, end_codice FROM usergroups
    	JOIN endpoints ON end_codice > 0
		ORDER BY usg_codice, end_codice
		ON CONFLICT ON CONSTRAINT per_codice DO NOTHING;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'end_after_trigger') THEN
			CREATE TRIGGER end_after_trigger
			AFTER INSERT OR UPDATE ON endpoints
			FOR EACH ROW
			EXECUTE PROCEDURE end_after_trigger_function();
		END IF;
	END
$$;


INSERT INTO endpoints (end_codice, end_desc )
VALUES
    (10, 'Gruppi Ditte'),
	(20, 'Ditte'),
    (30, 'Documenti Ditte'),
    (40, 'Scadenze Ditte'),
    (50, 'Immagini Ditte'),
    (60, 'Marchi'),
    (70, 'Tipologie Mezzi-Attrezzature'),
	(80, 'Verifiche Mezzi-Attrezzature'),
    (90, 'Modelli Mezzi-Attrezzature'),
    (100, 'Documenti Modelli Mezzi-Attrezzature'),
    (110, 'Video Modelli Mezzi-Attrezzature'),
    (120, 'Immagini Modelli Mezzi-Attrezzature'),
    (130, 'Check List Ditte'),
    (140, 'Check List Mezzi'),
    (150, 'Check List Cantieri'),
    (160, 'Check List Dipendenti'),
    (170, 'Mezzi-Attrezzature'),
    (180, 'Documenti Mezzi-Attrezzature'),
    (190, 'Manutenzioni Mezzi-Attrezzature'),
    (200, 'Scadenze Mezzi-Attrezzature'),
    (210, 'Video Mezzi-Attrezzature'),
    (220, 'Immagini Mezzi-Attrezzature'),
    (230, 'Utenti'),
    (240, 'Associazione Utenti <--> Ditte'),
    (250, 'Dipendenti'),
    (260, 'Documenti Dipendenti'),
    (270, 'Corsi Dipendenti'),
    (280, 'UNILAV Dipendenti'),
    (290, 'Scadenze Dipendenti'),
    (300, 'Immagini Dipendenti'),
    (310, 'Cantieri'),
    (320, 'Documenti Cantieri'),
    (330, 'Scadenze Cantieri'),
    (340, 'Immagini Cantieri'),
	(350, 'Giornale Lavori');

CREATE TABLE IF NOT EXISTS permessi
(
	per_usg    		INTEGER NOT NULL DEFAULT 1,
	per_end	    	INTEGER NOT NULL DEFAULT 1,
	per_view		SMALLINT NOT NULL  DEFAULT 0,
	per_add			SMALLINT NOT NULL  DEFAULT 0,
	per_update		SMALLINT NOT NULL  DEFAULT 0,
	per_delete		SMALLINT NOT NULL  DEFAULT 0,
	per_special1	SMALLINT NOT NULL  DEFAULT 0,
	per_special2	SMALLINT NOT NULL  DEFAULT 0,
	per_special3	SMALLINT NOT NULL  DEFAULT 0,
	per_created_at	TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	per_last_update	TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT per_codice PRIMARY KEY (per_usg, per_end),
	CONSTRAINT per_usg_fkey FOREIGN KEY (per_usg) REFERENCES usergroups (usg_codice) ON UPDATE CASCADE ON DELETE RESTRICT,
	CONSTRAINT dit_end_fkey FOREIGN KEY (per_end) REFERENCES endpoints (end_codice) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS per_usg ON permessi (per_usg);
CREATE INDEX IF NOT EXISTS per_end ON permessi (per_end);

CREATE OR REPLACE FUNCTION per_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.per_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.per_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'per_trigger') THEN
			CREATE TRIGGER per_trigger
			BEFORE INSERT OR UPDATE ON permessi
			FOR EACH ROW
			EXECUTE PROCEDURE per_trigger_function();
		END IF;
	END
$$;

/*
 	Creazione tabella di correlazione tra utenti e gruppi
 */
CREATE TABLE IF NOT EXISTS uteusg
(
    utg_ute    		INTEGER NOT NULL DEFAULT 1,
	utg_usg    		INTEGER NOT NULL DEFAULT 1,
	utg_created_at	TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	utg_last_update	TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT utg_codice PRIMARY KEY (utg_ute, utg_usg),
	CONSTRAINT utg_usg_fkey FOREIGN KEY (utg_usg) REFERENCES usergroups (usg_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT utg_ute_fkey FOREIGN KEY (utg_ute) REFERENCES utenti (ute_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS utg_usg ON uteusg (utg_usg);
CREATE INDEX IF NOT EXISTS utg_ute ON uteusg (utg_ute);

CREATE OR REPLACE FUNCTION utg_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.utg_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.utg_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'utg_trigger') THEN
			CREATE TRIGGER utg_trigger
			BEFORE INSERT OR UPDATE ON uteusg
			FOR EACH ROW
			EXECUTE PROCEDURE utg_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella gruppi
*/
CREATE TABLE IF NOT EXISTS gruppi
(
	gru_codice			BIGSERIAL PRIMARY KEY,
	gru_desc			VARCHAR (50) NOT NULL DEFAULT '',
	gru_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	gru_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	gru_user			INTEGER
);
CREATE INDEX gru_desc ON gruppi (gru_desc, gru_codice);

CREATE OR REPLACE FUNCTION gru_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.gru_desc		= TRIM(NEW.gru_desc);
		NEW.gru_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.gru_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'gru_trigger') THEN
			CREATE TRIGGER gru_trigger
			BEFORE INSERT OR UPDATE ON gruppi
			FOR EACH ROW
			EXECUTE PROCEDURE gru_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella ditte
*/
CREATE TABLE IF NOT EXISTS ditte
(
	dit_codice 		    INTEGER NOT NULL DEFAULT 1,
	dit_gru				INTEGER,
	dit_rag_soc1 		VARCHAR (50) NOT NULL DEFAULT '',
	dit_rag_soc2 		VARCHAR (50) NOT NULL DEFAULT '',
	dit_desc			VARCHAR (101) NOT NULL DEFAULT '',
	dit_indirizzo		VARCHAR (100) NOT NULL DEFAULT '',
	dit_citta			VARCHAR (50) NOT NULL DEFAULT '',
	dit_cap				VARCHAR (5) NOT NULL DEFAULT '',
	dit_prov			VARCHAR (2) NOT NULL DEFAULT '',
	dit_piva			VARCHAR (28) NOT NULL DEFAULT '',
	dit_codfis			VARCHAR (16) NOT NULL DEFAULT '',
	dit_note 			TEXT NOT NULL DEFAULT '',
	dit_riv				INTEGER,
	dit_email			VARCHAR (100) NOT NULL DEFAULT '',
    dit_pec				VARCHAR (100) NOT NULL DEFAULT '',
    dit_tel1			VARCHAR(15) NOT NULL DEFAULT '',
    dit_tel2			VARCHAR(15) NOT NULL DEFAULT '',
    dit_cel				VARCHAR(15) NOT NULL DEFAULT '',
    dit_matricola_inps	VARCHAR(10) NOT NULL DEFAULT '',
    dit_reseller		SMALLINT NOT NULL DEFAULT  0,
    dit_subappaltatrice	SMALLINT NOT NULL DEFAULT  0,
	dit_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	dit_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT dit_codice PRIMARY KEY (dit_codice),
);
CREATE INDEX dit_desc ON ditte (dit_desc, dit_codice);

CREATE OR REPLACE FUNCTION dit_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.dit_rag_soc1	= UPPER(TRIM(NEW.dit_rag_soc1));
	    NEW.dit_rag_soc2	= UPPER(TRIM(NEW.dit_rag_soc2));
	    NEW.dit_indirizzo	= UPPER(TRIM(NEW.dit_indirizzo));
	    NEW.dit_citta		= UPPER(TRIM(NEW.dit_citta));
	    NEW.dit_cap			= UPPER(TRIM(NEW.dit_cap));
	    NEW.dit_prov		= UPPER(TRIM(NEW.dit_prov));
	    NEW.dit_piva		= UPPER(TRIM(NEW.dit_piva));
	    NEW.dit_codfis		= UPPER(TRIM(NEW.dit_codfis));
		NEW.dit_desc     	= UPPER(TRIM(CONCAT(TRIM(NEW.dit_rag_soc1), ' ', TRIM(NEW.dit_rag_soc2))));
	    NEW.dit_note		= TRIM(NEW.dit_note);
	    NEW.dit_email		= LOWER(TRIM(NEW.dit_email));
	    NEW.dit_pec			= LOWER(TRIM(NEW.dit_pec));
	    NEW.dit_tel1		= UPPER(TRIM(NEW.dit_tel1));
	    NEW.dit_tel2		= UPPER(TRIM(NEW.dit_tel2));
	    NEW.dit_cel			= UPPER(TRIM(NEW.dit_cel));
		IF (NEW.dit_gru = 0) THEN
			NEW.dit_gru = NULL;
		END IF;
		IF (NEW.dit_riv = 0) THEN
			NEW.dit_riv = NULL;
		END IF;
		NEW.dit_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
        	NEW.dit_created_at	= NOW();
        END IF;
        EXECUTE FORMAT('CREATE TABLE IF NOT EXISTS %I PARTITION OF allegati FOR VALUES IN (%s)', 'allegati_' || NEW.dit_codice::TEXT, NEW.dit_codice);
		RETURN NEW;
	END;
$$ language 'plpgsql';

CREATE OR REPLACE FUNCTION dit_del_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
       	EXECUTE FORMAT('ALTER TABLE allegati DETACH PARTITION %I', 'allegati_' || OLD.dit_codice::TEXT);
       	EXECUTE FORMAT('DROP TABLE %I', 'allegati_' || OLD.dit_codice::TEXT);
		RETURN OLD;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'dit_trigger') THEN
			CREATE TRIGGER dit_trigger
			BEFORE INSERT OR UPDATE ON ditte
			FOR EACH ROW
			EXECUTE PROCEDURE dit_trigger_function();
		END IF;
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'dit_del_trigger') THEN
			CREATE TRIGGER dit_del_trigger
			AFTER DELETE ON ditte
			FOR EACH ROW
			EXECUTE PROCEDURE dit_del_trigger_function();
		END IF;
	END
$$;

/*
	Creazione Tabella Allegati

	all_type = 0 Documenti Clienti
	all_type = 1 Documenti Fornitori
	all_type = 2 Documenti Ditta
	all_type = 3 Documenti Pretiche
*/
CREATE TABLE IF NOT EXISTS allegati
(
    all_dit			INTEGER NOT NULL,
    all_type		SMALLINT NOT NULL DEFAULT 0,
	all_doc			INTEGER NOT NULL,
	all_idx			INTEGER NOT NULL DEFAULT 0,
	all_desc		VARCHAR (100) NOT NULL DEFAULT '',
	all_fname		VARCHAR (260) NOT NULL DEFAULT '',
	all_local_fname	VARCHAR (260) NOT NULL DEFAULT '',
	all_bytes_size	BIGINT NOT NULL DEFAULT 0,
	all_date_time	TIMESTAMP DEFAULT NULL,
	all_created_at	TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	all_last_update	TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	all_type_doc    INTEGER,
	all_type_dof    INTEGER,
	all_type_dod    INTEGER,
	all_type_dop    INTEGER,
	CONSTRAINT all_codice PRIMARY KEY (all_dit, all_type, all_doc, all_idx),
	CONSTRAINT all_ditte_fkey FOREIGN KEY (all_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT all_doccli_fkey FOREIGN KEY (all_type_doc) REFERENCES clienti (cli_codice) ON UPDATE CASCADE ON DELETE CASCADE
)  PARTITION BY LIST (all_dit);

ALTER TABLE allegati ADD CONSTRAINT all_doccli_fkey FOREIGN KEY (all_type_doc) REFERENCES clienti (cli_codice) ON UPDATE CASCADE ON DELETE CASCADE;


/*

 	CONSTRAINT all_documenti_fkey FOREIGN KEY (all_dit, all_type_doc) REFERENCES documenti (doc_dit, doc_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT all_doccantieri_fkey FOREIGN KEY (all_dit, all_type_dca) REFERENCES doccantieri (dca_dit, dca_codice) ON UPDATE CASCADE ON DELETE CASCADE,
    CONSTRAINT all_docditte_fkey FOREIGN KEY (all_dit, all_type_dod) REFERENCES docditte (dod_dit, dod_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT all_docmezzi_fkey FOREIGN KEY (all_dit, all_type_dme) REFERENCES docmezzi (dme_dit, dme_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT all_manutenzioni_fkey FOREIGN KEY (all_dit, all_type_mnt) REFERENCES manutenzioni (mnt_dit, mnt_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT all_docmodelli_fkey FOREIGN KEY (all_dit, all_type_dmo) REFERENCES docmodelli (dmo_dit, dmo_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT all_giornale_fkey FOREIGN KEY (all_dit, all_type_gio) REFERENCES giornalelav (gio_dit, gio_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT all_certificatipag_fkey FOREIGN KEY (all_dit, all_type_cpa) REFERENCES certificatipag (cpa_dit, cpa_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT all_dipvisite_fkey FOREIGN KEY (all_dit, all_type_dvi) REFERENCES dipvisite (dvi_dit, dvi_codice) ON UPDATE CASCADE ON DELETE CASCADE

 */

CREATE INDEX IF NOT EXISTS all_ditte ON allegati (all_dit);
CREATE INDEX IF NOT EXISTS all_documenti ON allegati (all_dit, all_type_doc);

/*
CREATE INDEX IF NOT EXISTS all_doccantieri ON allegati (all_dit, all_type_dca);
CREATE INDEX IF NOT EXISTS all_docditte ON allegati (all_dit, all_type_dod);
CREATE INDEX IF NOT EXISTS all_docmezzi ON allegati (all_dit, all_type_dme);
CREATE INDEX IF NOT EXISTS all_manutenzioni ON allegati (all_dit, all_type_mnt);
CREATE INDEX IF NOT EXISTS all_docmodelli ON allegati (all_dit, all_type_dmo);
CREATE INDEX IF NOT EXISTS all_giornalelav ON allegati (all_dit, all_type_gio);
CREATE INDEX IF NOT EXISTS all_certificatipag ON allegati (all_dit, all_type_cpa);
CREATE INDEX IF NOT EXISTS all_dipvisite ON allegati (all_dit, all_type_dvi);
*/

CREATE OR REPLACE FUNCTION all_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.all_fname		= TRIM(NEW.all_fname);
		NEW.all_last_update	= NOW();
	    IF  (TG_OP = 'INSERT') THEN
           NEW.all_created_at	= NOW();
    	END IF;
		IF (NEW.all_type = 0) THEN
            NEW.all_type_doc = NEW.all_doc;
            NEW.all_type_dof = NULL;
            NEW.all_type_dod = NULL;
            NEW.all_type_dop = NULL;
        END IF;
		IF (NEW.all_type = 1) THEN
            NEW.all_type_doc = NULL;
            NEW.all_type_dof = NEW.all_doc;
            NEW.all_type_dod = NULL;
            NEW.all_type_dop = NULL;
        END IF;
		IF (NEW.all_type = 2) THEN
            NEW.all_type_doc = NULL;
            NEW.all_type_dof = NULL;
            NEW.all_type_dod = NEW.all_doc;
            NEW.all_type_dop = NULL;
        END IF;
		IF (NEW.all_type = 3) THEN
            NEW.all_type_doc = NULL;
            NEW.all_type_dof = NULL;
            NEW.all_type_dod = NULL;
            NEW.all_type_dop = NEW.all_doc;
        END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'all_trigger') THEN
			CREATE TRIGGER all_trigger
			BEFORE INSERT OR UPDATE ON allegati
			FOR EACH ROW
			EXECUTE PROCEDURE all_trigger_function();
		END IF;
	END
$$;

/*
	Tabella Immagini ditte
*/
CREATE TABLE IF NOT EXISTS imgditte
(
	img_dit			INTEGER NOT NULL DEFAULT 0,
	img_codice		INTEGER NOT NULL,
	img_formato		SMALLINT NOT NULL DEFAULT 0,
	img_tipo		SMALLINT NOT NULL DEFAULT 0,
	img_bytes_size	INTEGER NOT NULL DEFAULT 0,
	img_created_at	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_last_update	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_data		BYTEA,
	CONSTRAINT imd_codice PRIMARY KEY (img_dit, img_codice, img_formato),
	CONSTRAINT imd_dit_fkey FOREIGN KEY (img_codice) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS imd_ditte ON imgditte (img_dit, img_codice);

CREATE OR REPLACE FUNCTION imd_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.img_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.img_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'imd_trigger') THEN
			CREATE TRIGGER imd_trigger
			BEFORE INSERT OR UPDATE ON imgditte
			FOR EACH ROW
			EXECUTE PROCEDURE imd_trigger_function();
		END IF;
	END
$$;


/*
*	Creazione Tabella Ditte Utenti
*/
CREATE TABLE IF NOT EXISTS uteditte
(
    utd_dit				INTEGER,
	utd_ute 		    INTEGER,
	utd_default			SMALLINT NOT NULL DEFAULT 0,
	utd_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	utd_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT utd_ditute PRIMARY KEY (utd_dit, utd_ute),
	CONSTRAINT utd_ditte_fkey FOREIGN KEY (utd_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT utd_utente_fkey FOREIGN KEY (utd_ute) REFERENCES utenti (ute_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX utd_dit ON uteditte (utd_dit);
CREATE INDEX utd_ute ON uteditte (utd_ute, utd_default);

CREATE OR REPLACE FUNCTION utd_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.utd_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.utd_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'utd_trigger') THEN
			CREATE TRIGGER utd_trigger
			BEFORE INSERT OR UPDATE ON uteditte
			FOR EACH ROW
			EXECUTE PROCEDURE utd_trigger_function();
		END IF;
	END
$$;

INSERT INTO uteditte (utd_dit, utd_ute)
VALUES (1, 1);
INSERT INTO uteditte (utd_dit, utd_ute)
VALUES (1, 2);
INSERT INTO uteditte (utd_dit, utd_ute)
VALUES (1, 3);

/*

*/
CREATE TABLE IF NOT EXISTS sedditte
(
    sed_dit					INTEGER NOT NULL,
	sed_codice				INTEGER NOT NULL DEFAULT 1,
	sed_indirizzo			VARCHAR(100) NOT NULL DEFAULT '',
	sed_citta				VARCHAR(50) NOT NULL DEFAULT '',
	sed_cap					VARCHAR(5) NOT NULL DEFAULT '',
	sed_prov				VARCHAR(2) NOT NULL DEFAULT '',
	sed_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	sed_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	sed_user				INTEGER NOT NULL DEFAULT 0,
	CONSTRAINT sed_codice PRIMARY KEY (sed_dit, sed_codice),
	CONSTRAINT sed_ditte_fkey FOREIGN KEY (sed_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS sed_ditte ON sedditte (sed_dit, sed_codice);
CREATE INDEX IF NOT EXISTS sed_citta ON sedditte (sed_citta, sed_indirizzo, sed_dit, sed_codice);

CREATE OR REPLACE FUNCTION sed_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.sed_indirizzo	= TRIM(NEW.sed_indirizzo);
		NEW.sed_citta		= TRIM(NEW.sed_citta);
		NEW.sed_cap		    = TRIM(NEW.sed_cap);
		NEW.sed_prov		= TRIM(NEW.sed_prov);
		NEW.sed_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.sed_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'sed_trigger') THEN
			CREATE TRIGGER sed_trigger
			BEFORE INSERT OR UPDATE ON sedditte
			FOR EACH ROW
			EXECUTE PROCEDURE sed_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella persone_giuridiche
*/
CREATE TABLE IF NOT EXISTS persone_giuridiche
(
	pgi_codice			BIGSERIAL PRIMARY KEY,
	pgi_desc			VARCHAR (50) NOT NULL DEFAULT '',
	pgi_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	pgi_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	pgi_user			INTEGER
);
CREATE INDEX pgi_desc ON persone_giuridiche (pgi_desc, pgi_codice);

CREATE OR REPLACE FUNCTION pgi_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.pgi_desc		= TRIM(NEW.pgi_desc);
		NEW.pgi_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.pgi_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'pgi_trigger') THEN
			CREATE TRIGGER pgi_trigger
			BEFORE INSERT OR UPDATE ON persone_giuridiche
			FOR EACH ROW
			EXECUTE PROCEDURE pgi_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella Tipologia Attività (Imprese)
*/
CREATE TABLE IF NOT EXISTS tipologia_attivita
(
	tat_codice			BIGSERIAL PRIMARY KEY,
	tat_desc			VARCHAR (50) NOT NULL DEFAULT '',
	tat_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	tat_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	tat_user			INTEGER
);
CREATE INDEX tat_desc ON tipologia_attivita (tat_desc, tat_codice);

CREATE OR REPLACE FUNCTION tat_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.tat_desc		= TRIM(NEW.tat_desc);
		NEW.tat_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.tat_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'tat_trigger') THEN
			CREATE TRIGGER tat_trigger
			BEFORE INSERT OR UPDATE ON tipologia_attivita
			FOR EACH ROW
			EXECUTE PROCEDURE tat_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella Attività (Privati)
*/
CREATE TABLE IF NOT EXISTS attivita
(
	att_codice			BIGSERIAL PRIMARY KEY,
	att_desc			VARCHAR (50) NOT NULL DEFAULT '',
	att_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	att_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	att_user			INTEGER
);
CREATE INDEX att_desc ON attivita (att_desc, att_codice);

CREATE OR REPLACE FUNCTION att_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.att_desc		= TRIM(NEW.att_desc);
		NEW.att_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.att_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'att_trigger') THEN
			CREATE TRIGGER att_trigger
			BEFORE INSERT OR UPDATE ON attivita
			FOR EACH ROW
			EXECUTE PROCEDURE att_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella Finalita
*/
CREATE TABLE IF NOT EXISTS finalita
(
	fin_codice			BIGSERIAL PRIMARY KEY,
	fin_desc			VARCHAR (50) NOT NULL DEFAULT '',
	fin_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	fin_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	fin_user			INTEGER
);
CREATE INDEX fin_desc ON finalita (fin_desc, fin_codice);

CREATE OR REPLACE FUNCTION fin_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.fin_desc		= TRIM(NEW.fin_desc);
		NEW.fin_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.fin_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'fin_trigger') THEN
			CREATE TRIGGER fin_trigger
			BEFORE INSERT OR UPDATE ON finalita
			FOR EACH ROW
			EXECUTE PROCEDURE fin_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella settori
*/
CREATE TABLE IF NOT EXISTS settori
(
	set_codice			BIGSERIAL PRIMARY KEY,
	set_desc			VARCHAR (50) NOT NULL DEFAULT '',
	set_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	set_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	set_user			INTEGER
);
CREATE INDEX set_desc ON settori (set_desc, set_codice);

CREATE OR REPLACE FUNCTION set_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.set_desc		= TRIM(NEW.set_desc);
		NEW.set_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.set_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'set_trigger') THEN
			CREATE TRIGGER set_trigger
			BEFORE INSERT OR UPDATE ON settori
			FOR EACH ROW
			EXECUTE PROCEDURE set_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella strumenti
*/
CREATE TABLE IF NOT EXISTS strumenti
(
	str_codice			BIGSERIAL PRIMARY KEY,
	str_desc			VARCHAR (50) NOT NULL DEFAULT '',
	str_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	str_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	str_user			INTEGER
);
CREATE INDEX str_desc ON strumenti (str_desc, str_codice);

CREATE OR REPLACE FUNCTION str_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.str_desc		= TRIM(NEW.str_desc);
		NEW.str_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.str_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'str_trigger') THEN
			CREATE TRIGGER str_trigger
			BEFORE INSERT OR UPDATE ON strumenti
			FOR EACH ROW
			EXECUTE PROCEDURE str_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella garanzie
*/
CREATE TABLE IF NOT EXISTS garanzie
(
	gar_codice			BIGSERIAL PRIMARY KEY,
	gar_desc			VARCHAR (50) NOT NULL DEFAULT '',
	gar_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	gar_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	gar_user			INTEGER
);
CREATE INDEX IF NOT EXISTS gar_desc ON garanzie (gar_desc, gar_codice);

CREATE OR REPLACE FUNCTION gar_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.gar_desc		= TRIM(NEW.gar_desc);
		NEW.gar_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.gar_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'gar_trigger') THEN
			CREATE TRIGGER gar_trigger
			BEFORE INSERT OR UPDATE ON garanzie
			FOR EACH ROW
			EXECUTE PROCEDURE gar_trigger_function();
		END IF;
	END
$$;


/*
*	Creazione Tabella fabbisogno
*/
CREATE TABLE IF NOT EXISTS fabbisogno
(
	fab_codice			BIGSERIAL PRIMARY KEY,
	fab_desc			VARCHAR (50) NOT NULL DEFAULT '',
	fab_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	fab_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	fab_user			INTEGER
);
CREATE INDEX fab_desc ON fabbisogno (fab_desc, fab_codice);

CREATE OR REPLACE FUNCTION fab_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.fab_desc		= TRIM(NEW.fab_desc);
		NEW.fab_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.fab_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'fab_trigger') THEN
			CREATE TRIGGER fab_trigger
			BEFORE INSERT OR UPDATE ON fabbisogno
			FOR EACH ROW
			EXECUTE PROCEDURE fab_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella controparti
*/
CREATE TABLE IF NOT EXISTS controparti
(
	cnt_codice			BIGSERIAL PRIMARY KEY,
	cnt_desc			VARCHAR (100) NOT NULL DEFAULT '',
	cnt_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	cnt_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	cnt_user			INTEGER
);
CREATE INDEX cnt_desc ON controparti (cnt_desc, cnt_codice);

CREATE OR REPLACE FUNCTION cnt_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.cnt_desc		= TRIM(NEW.cnt_desc);
		NEW.cnt_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.cnt_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'cnt_trigger') THEN
			CREATE TRIGGER cnt_trigger
			BEFORE INSERT OR UPDATE ON controparti
			FOR EACH ROW
			EXECUTE PROCEDURE cnt_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella Clienti
*/
CREATE TABLE IF NOT EXISTS clienti
(
	cli_codice				BIGSERIAL PRIMARY KEY,
	cli_rag_soc1			VARCHAR (100)  NOT NULL DEFAULT '',
	cli_rag_soc2			VARCHAR (100)  NOT NULL DEFAULT '',
	cli_desc				VARCHAR (201)  NOT NULL DEFAULT '',
	cli_tipo				SMALLINT NOT NULL DEFAULT 0,
	cli_indirizzo			VARCHAR (100)  NOT NULL DEFAULT '',
	cli_citta				VARCHAR (200)  NOT NULL DEFAULT '',
	cli_cap					VARCHAR (5)    NOT NULL DEFAULT '',
	cli_prov				VARCHAR (2)    NOT NULL DEFAULT '',
	cli_piva				VARCHAR (28)   NOT NULL DEFAULT '',
	cli_codfis				VARCHAR (16)   NOT NULL DEFAULT '',
	cli_email				VARCHAR (100)  NOT NULL DEFAULT '',
	cli_web					TEXT NOT NULL DEFAULT '',
	cli_pec					VARCHAR (100)  NOT NULL DEFAULT '',
	cli_tel1				VARCHAR (15)   NOT NULL DEFAULT '',
	cli_tel2				VARCHAR (15)   NOT NULL DEFAULT '',
	cli_cel					VARCHAR (15)   NOT NULL DEFAULT '',
	cli_pgi					BIGINT,
	cli_gru					INTEGER,
	cli_tat					BIGINT,
	cli_inizio_attivita		DATE,
	cli_ateco1				VARCHAR(20) NOT NULL DEFAULT '',
	cli_ateco2				VARCHAR(20) NOT NULL DEFAULT '',
	cli_interlocutore		VARCHAR(100) NOT NULL DEFAULT '',
	cli_int_funzione		VARCHAR(100) NOT NULL DEFAULT '',
	cli_int_telefono		VARCHAR(15) NOT NULL DEFAULT '',
	cli_int_email			VARCHAR (100)  NOT NULL DEFAULT '',
	cli_data_nascita		DATE,
	cli_luogo_nascita		VARCHAR(100) NOT NULL DEFAULT '',
	cli_prov_nascita		VARCHAR(2) NOT NULL DEFAULT '',
	cli_cap_nascita			VARCHAR (5)    NOT NULL DEFAULT '',
	cli_att					BIGINT,
	cli_note				TEXT NOT NULL DEFAULT '',
	cli_capitale_sociale 	DOUBLE PRECISION NOT NULL DEFAULT 0,
	cli_protesti 			SMALLINT NOT NULL DEFAULT 0,
	cli_cronaca_giud 		SMALLINT NOT NULL DEFAULT 0,
	cli_note_reputazione	TEXT NOT NULL DEFAULT '',
	cli_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	cli_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	cli_user				INTEGER,
	CONSTRAINT clienti_pgi_fkey FOREIGN KEY (cli_pgi) REFERENCES persone_giuridiche (pgi_codice) ON UPDATE CASCADE ON DELETE RESTRICT,
	CONSTRAINT clienti_gru_fkey FOREIGN KEY (cli_gru) REFERENCES gruppi (gru_codice) ON UPDATE CASCADE ON DELETE RESTRICT,
	CONSTRAINT clienti_tat_fkey FOREIGN KEY (cli_tat) REFERENCES tipologia_attivita (tat_codice) ON UPDATE CASCADE ON DELETE RESTRICT,
	CONSTRAINT clienti_att_fkey FOREIGN KEY (cli_att) REFERENCES attivita (att_codice) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS cli_desc ON clienti (cli_desc, cli_codice);
CREATE INDEX IF NOT EXISTS cli_pgi ON clienti (cli_pgi, cli_codice);
CREATE INDEX IF NOT EXISTS cli_gru ON clienti (cli_gru, cli_codice);
CREATE INDEX IF NOT EXISTS cli_tat ON clienti (cli_tat, cli_codice);
CREATE INDEX IF NOT EXISTS cli_att ON clienti (cli_att, cli_codice);

ALTER TABLE clienti
	ADD COLUMN IF NOT EXISTS cli_capitale_sociale 	DOUBLE PRECISION NOT NULL DEFAULT 0,
	ADD COLUMN IF NOT EXISTS cli_protesti 			SMALLINT NOT NULL DEFAULT 0,
	ADD COLUMN IF NOT EXISTS cli_cronaca_giud 		SMALLINT NOT NULL DEFAULT 0,
	ADD COLUMN IF NOT EXISTS cli_note_reputazione	TEXT NOT NULL DEFAULT '';

ALTER TABLE clienti
	DROP COLUMN IF EXISTS cli_capitale_sociale;

CREATE OR REPLACE FUNCTION cli_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.cli_rag_soc1		= TRIM(NEW.cli_rag_soc1);
	    NEW.cli_rag_soc2		= TRIM(NEW.cli_rag_soc2);
	    NEW.cli_desc			= TRIM(NEW.cli_rag_soc1 || ' ' || NEW.cli_rag_soc2);
	    NEW.cli_indirizzo		= TRIM(NEW.cli_indirizzo);
	    NEW.cli_citta			= TRIM(NEW.cli_citta);
	    NEW.cli_cap				= TRIM(NEW.cli_cap);
	    NEW.cli_prov			= TRIM(NEW.cli_prov);
	    NEW.cli_piva			= TRIM(NEW.cli_piva);
	    NEW.cli_codfis			= TRIM(NEW.cli_codfis);
	    NEW.cli_email			= LOWER(TRIM(NEW.cli_email));
	    NEW.cli_web				= TRIM(NEW.cli_web);
	    NEW.cli_pec				= LOWER(TRIM(NEW.cli_pec));
		NEW.cli_tel1			= TRIM(NEW.cli_tel1);
	    NEW.cli_tel2			= TRIM(NEW.cli_tel2);
	    NEW.cli_cel				= TRIM(NEW.cli_cel);
	    NEW.cli_ateco1			= TRIM(NEW.cli_ateco1);
	    NEW.cli_ateco2			= TRIM(NEW.cli_ateco2);
	    NEW.cli_interlocutore	= TRIM(NEW.cli_interlocutore);
	    NEW.cli_int_funzione	= TRIM(NEW.cli_int_funzione);
	    NEW.cli_int_telefono	= TRIM(NEW.cli_int_telefono);
	    NEW.cli_int_email		= LOWER(TRIM(NEW.cli_int_email));
	    NEW.cli_luogo_nascita	= TRIM(NEW.cli_luogo_nascita);
	    NEW.cli_prov_nascita	= TRIM(NEW.cli_prov_nascita);
	    NEW.cli_cap_nascita		= TRIM(NEW.cli_cap_nascita);
	    NEW.cli_note			= TRIM(NEW.cli_note);
		NEW.cli_last_update		= NOW();
	    if (NEW.cli_pgi = 0) THEN
			NEW.cli_pgi = NULL;
		END IF;
	    if (NEW.cli_gru = 0) THEN
			NEW.cli_gru = NULL;
		END IF;
	    if (NEW.cli_tat = 0) THEN
			NEW.cli_tat = NULL;
		END IF;
	    if (NEW.cli_att = 0) THEN
			NEW.cli_att = NULL;
		END IF;
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.cli_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'cli_trigger') THEN
			CREATE TRIGGER cli_trigger
			BEFORE INSERT OR UPDATE ON clienti
			FOR EACH ROW
			EXECUTE PROCEDURE cli_trigger_function();
		END IF;
	END
$$;

/*
	Tabella Immagini imgclienti
*/
CREATE TABLE IF NOT EXISTS imgclienti
(
	img_dit			BIGINT NOT NULL DEFAULT 0,
	img_codice		BIGINT NOT NULL CHECK(img_codice > 0),
	img_formato		SMALLINT NOT NULL DEFAULT 0,
	img_tipo		SMALLINT NOT NULL DEFAULT 0,
	img_bytes_size	INTEGER NOT NULL DEFAULT 0,
	img_created_at	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_last_update	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_data		BYTEA,
	CONSTRAINT imc_codice PRIMARY KEY (img_dit, img_codice, img_formato),
	CONSTRAINT imc_dit_fkey FOREIGN KEY (img_dit) REFERENCES clienti (cli_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT imc_cli_fkey FOREIGN KEY (img_codice) REFERENCES clienti (cli_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS imc_ditte ON imgclienti (img_dit, img_codice);
CREATE INDEX IF NOT EXISTS imc_clienti ON imgclienti (img_codice);

CREATE OR REPLACE FUNCTION imc_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.img_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.img_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'imc_trigger') THEN
			CREATE TRIGGER imc_trigger
			BEFORE INSERT OR UPDATE ON imgclienti
			FOR EACH ROW
			EXECUTE PROCEDURE imc_trigger_function();
		END IF;
	END
$$;

/*
 	Tabella Comuni
 */
 /*
CREATE TABLE IF NOT EXISTS comuni
(
    com_codice_regione				INTEGER NOT NULL DEFAULT 0,
    com_codice_citta_metropolitana	INTEGER NOT NULL DEFAULT 0,
	com_codice_provincia			INTEGER NOT NULL DEFAULT 0,
	com_codice						INTEGER NOT NULL DEFAULT 0,
	com_desc						VARCHAR(50) NOT NULL DEFAULT '',
	com_ripartizione_geofrafica		VARCHAR(30) NOT NULL DEFAULT 0,
	com_desc_regione				VARCHAR(30) NOT NULL DEFAULT '',
	com_desc_citta_metropolitana	VARCHAR(30) NOT NULL DEFAULT '',
	com_prov						VARCHAR(2) NOT NULL DEFAULT '',
	com_codice_catastale			VARCHAR(4) NOT NULL DEFAULT '',
	com_cap							VARCHAR(5) DEFAULT '',
	com_created_at					TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	com_last_update					TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	com_user						INTEGER,
	CONSTRAINT com_codice PRIMARY KEY (com_codice),
	CONSTRAINT comuni_com_codice_check CHECK (com_codice >= 0)
);
CREATE INDEX IF NOT EXISTS com_desc ON comuni (com_desc, com_codice);
CREATE INDEX IF NOT EXISTS com_prov ON comuni (com_prov, com_codice);

CREATE OR REPLACE FUNCTION com_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.com_desc		= TRIM(NEW.com_desc);
	    NEW.com_prov		= TRIM(NEW.com_prov);
	    NEW.com_codice_catastale	= TRIM(NEW.com_codice_catastale);
	    NEW.com_cap			= TRIM(NEW.com_cap);
	    NEW.com_ripartizione_geofrafica	= TRIM(NEW.com_ripartizione_geofrafica);
	    NEW.com_desc_regione	= TRIM(NEW.com_desc_regione);
	    NEW.com_desc_citta_metropolitana	= TRIM(NEW.com_desc_citta_metropolitana);
		NEW.com_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.com_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'com_trigger') THEN
			CREATE TRIGGER com_trigger
			BEFORE INSERT OR UPDATE ON comuni
			FOR EACH ROW
			EXECUTE PROCEDURE com_trigger_function();
		END IF;
	END
$$;

*/

/*
	Tabella Reputazione
*/
CREATE TABLE IF NOT EXISTS reputazione
(
    scd_dit					INTEGER NOT NULL,
	scd_codice				INTEGER NOT NULL DEFAULT 1,
	scd_data				DATE,
	scd_desc				VARCHAR(512) NOT NULL DEFAULT '',
	scd_scad_alert_before	SMALLINT NOT NULL DEFAULT 0,
	scd_scad_alert_after	SMALLINT NOT NULL DEFAULT 0,
	scd_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	scd_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT scd_codice PRIMARY KEY (scd_dit, scd_codice),
	CONSTRAINT scd_ditte_fkey FOREIGN KEY (scd_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX scd_desc ON scaditte (scd_desc, scd_dit, scd_codice);

CREATE OR REPLACE FUNCTION scd_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.scd_desc		= TRIM(NEW.scd_desc);
		NEW.scd_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.scd_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'scd_trigger') THEN
			CREATE TRIGGER scd_trigger
			BEFORE INSERT OR UPDATE ON scaditte
			FOR EACH ROW
			EXECUTE PROCEDURE scd_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella fornitori
*/
CREATE TABLE IF NOT EXISTS fornitori
(
	for_codice 	    BIGSERIAL PRIMARY KEY,
	for_rag_soc1 		VARCHAR (50) NOT NULL DEFAULT '',
	for_rag_soc2 		VARCHAR (50) NOT NULL DEFAULT '',
	for_desc			VARCHAR (101) NOT NULL DEFAULT '',
	for_indirizzo		VARCHAR (100) NOT NULL DEFAULT '',
	for_citta			VARCHAR (50) NOT NULL DEFAULT '',
	for_cap				VARCHAR (5) NOT NULL DEFAULT '',
	for_prov			VARCHAR (2) NOT NULL DEFAULT '',
	for_piva			VARCHAR (28) NOT NULL DEFAULT '',
	for_codfis			VARCHAR (16) NOT NULL DEFAULT '',
	for_note 			TEXT NOT NULL DEFAULT '',
	for_email			VARCHAR (100) NOT NULL DEFAULT '',
    for_pec				VARCHAR (100) NOT NULL DEFAULT '',
    for_tel1			VARCHAR(15) NOT NULL DEFAULT '',
    for_tel2			VARCHAR(15) NOT NULL DEFAULT '',
    for_cel				VARCHAR(15) NOT NULL DEFAULT '',
	for_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	for_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	for_user			INTEGER
);
CREATE INDEX IF NOT EXISTS for_desc ON fornitori (for_desc, for_codice);

CREATE OR REPLACE FUNCTION for_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.for_rag_soc1	= TRIM(NEW.for_rag_soc1);
	    NEW.for_rag_soc2	= TRIM(NEW.for_rag_soc2);
	    NEW.for_indirizzo	= TRIM(NEW.for_indirizzo);
	    NEW.for_citta		= TRIM(NEW.for_citta);
	    NEW.for_cap			= TRIM(NEW.for_cap);
	    NEW.for_prov		= TRIM(NEW.for_prov);
	    NEW.for_piva		= TRIM(NEW.for_piva);
	    NEW.for_codfis		= TRIM(NEW.for_codfis);
		NEW.for_desc     	= TRIM(CONCAT(TRIM(NEW.for_rag_soc1), ' ', TRIM(NEW.for_rag_soc2)));
	    NEW.for_note		= TRIM(NEW.for_note);
	    NEW.for_email		= LOWER(TRIM(NEW.for_email));
	    NEW.for_pec			= LOWER(TRIM(NEW.for_pec));
	    NEW.for_tel1		= TRIM(NEW.for_tel1);
	    NEW.for_tel2		= TRIM(NEW.for_tel2);
	    NEW.for_cel			= TRIM(NEW.for_cel);
		NEW.for_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
        	NEW.for_created_at	= NOW();
        END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'for_trigger') THEN
			CREATE TRIGGER for_trigger
			BEFORE INSERT OR UPDATE ON fornitori
			FOR EACH ROW
			EXECUTE PROCEDURE for_trigger_function();
		END IF;
	END
$$;

/*
	Tabella Immagini imgfornitori
*/
CREATE TABLE IF NOT EXISTS imgfornitori
(
	img_dit			BIGINT NOT NULL DEFAULT 0,
	img_codice		BIGINT NOT NULL CHECK(img_codice > 0),
	img_formato		SMALLINT NOT NULL DEFAULT 0,
	img_tipo		SMALLINT NOT NULL DEFAULT 0,
	img_bytes_size	INTEGER NOT NULL DEFAULT 0,
	img_created_at	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_last_update	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_data		BYTEA,
	CONSTRAINT imf_codice PRIMARY KEY (img_dit, img_codice, img_formato),
	CONSTRAINT imf_dit_fkey FOREIGN KEY (img_dit) REFERENCES fornitori (for_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT imf_for_fkey FOREIGN KEY (img_codice) REFERENCES fornitori (for_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS imf_ditte ON imgfornitori (img_dit, img_codice);
CREATE INDEX IF NOT EXISTS imf_fornitori ON imgfornitori (img_codice);

CREATE OR REPLACE FUNCTION imf_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.img_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.img_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'imf_trigger') THEN
			CREATE TRIGGER imf_trigger
			BEFORE INSERT OR UPDATE ON imgfornitori
			FOR EACH ROW
			EXECUTE PROCEDURE imf_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella componenti_famiglie
*/
CREATE TABLE IF NOT EXISTS componenti
(
	cfa_codice 	    	BIGSERIAL PRIMARY KEY,
	cfa_cli				BIGINT,
	cfa_att				BIGINT,
	cfa_cognome 		VARCHAR (50) NOT NULL DEFAULT '',
	cfa_nome	 		VARCHAR (50) NOT NULL DEFAULT '',
	cfa_desc			VARCHAR (101) NOT NULL DEFAULT '',
	cfa_data_nascita	DATE,
	cfa_luogo_nascita	VARCHAR (100) NOT NULL DEFAULT '',
	cfa_prov_nascita	VARCHAR (2) NOT NULL DEFAULT '',
	cfa_cap_nascita		VARCHAR (5) NOT NULL DEFAULT '',
	cfa_codfis			VARCHAR (16) NOT NULL DEFAULT '',
	cfa_parentela		VARCHAR (100) NOT NULL DEFAULT '',
	cfa_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	cfa_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	cfa_user			INTEGER,
	CONSTRAINT cfa_cli_fkey FOREIGN KEY (cfa_cli) REFERENCES clienti (cli_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT cfa_att_fkey FOREIGN KEY (cfa_att) REFERENCES attivita (att_codice) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE INDEX cfa_desc ON componenti (cfa_desc, cfa_codice);
CREATE INDEX cfa_cliente ON componenti (cfa_cli, cfa_codice);
CREATE INDEX cfa_attivita ON componenti (cfa_att, cfa_codice);

CREATE OR REPLACE FUNCTION cfa_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.cfa_cognome			= TRIM(NEW.cfa_cognome);
	    NEW.cfa_nome			= TRIM(NEW.cfa_nome);
	    NEW.cfa_desc			= TRIM(CONCAT(TRIM(NEW.cfa_cognome), ' ', TRIM(NEW.cfa_nome)));
	    NEW.cfa_luogo_nascita	= TRIM(NEW.cfa_luogo_nascita);
	    NEW.cfa_prov_nascita	= TRIM(NEW.cfa_prov_nascita);
	    NEW.cfa_cap_nascita		= TRIM(NEW.cfa_cap_nascita);
	    NEW.cfa_codfis			= UPPER(TRIM(NEW.cfa_codfis));
	    NEW.cfa_parentela		= TRIM(NEW.cfa_parentela);
		NEW.cfa_last_update	= NOW();
	    IF NEW.cfa_att = 0 THEN
			NEW.cfa_att = NULL;
		END IF;
   	    IF  (TG_OP = 'INSERT') THEN
        	NEW.cfa_created_at	= NOW();
        END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'cfa_trigger') THEN
			CREATE TRIGGER cfa_trigger
			BEFORE INSERT OR UPDATE ON componenti
			FOR EACH ROW
			EXECUTE PROCEDURE cfa_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella Soci
*/
CREATE TABLE IF NOT EXISTS soci
(
	soc_codice 	    	BIGSERIAL PRIMARY KEY,
	soc_cli				BIGINT,
	soc_desc 			VARCHAR (101) NOT NULL DEFAULT '',
	soc_data_nascita	DATE,
	soc_luogo_nascita	VARCHAR (100) NOT NULL DEFAULT '',
	soc_prov_nascita	VARCHAR (2) NOT NULL DEFAULT '',
	soc_cap_nascita		VARCHAR (5) NOT NULL DEFAULT '',
	soc_codfis			VARCHAR (16) NOT NULL DEFAULT '',
	soc_esposto			SMALLINT NOT NULL DEFAULT 0,
	soc_disponibile		SMALLINT NOT NULL DEFAULT 0,
	soc_percentuale		DOUBLE PRECISION NOT NULL DEFAULT 0,
	soc_funzione		SMALLINT NOT NULL DEFAULT 0,
	soc_note			TEXT NOT NULL DEFAULT '',
	soc_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	soc_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	soc_user			INTEGER,
	CONSTRAINT soc_cli_fkey FOREIGN KEY (soc_cli) REFERENCES clienti (cli_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX soc_desc ON soci (soc_desc, soc_codice);
CREATE INDEX soc_cliente ON soci (soc_cli, soc_codice);

CREATE OR REPLACE FUNCTION soc_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.soc_desc			= TRIM(NEW.soc_desc);
	    NEW.soc_luogo_nascita	= TRIM(NEW.soc_luogo_nascita);
	    NEW.soc_prov_nascita	= TRIM(NEW.soc_prov_nascita);
	    NEW.soc_cap_nascita		= TRIM(NEW.soc_cap_nascita);
	    NEW.soc_codfis			= UPPER(TRIM(NEW.soc_codfis));
	    NEW.soc_note			= TRIM(NEW.soc_note);
		NEW.soc_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
        	NEW.soc_created_at	= NOW();
        END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'soc_trigger') THEN
			CREATE TRIGGER soc_trigger
			BEFORE INSERT OR UPDATE ON soci
			FOR EACH ROW
			EXECUTE PROCEDURE soc_trigger_function();
		END IF;
	END
$$;


/*
*	Creazione Tabella poteri
*/
CREATE TABLE IF NOT EXISTS poteri
(
	pot_codice			BIGSERIAL PRIMARY KEY,
	pot_desc			VARCHAR (100) NOT NULL DEFAULT '',
	pot_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	pot_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	pot_user			INTEGER
);
CREATE INDEX IF NOT EXISTS pot_desc ON poteri (pot_desc, pot_codice);

CREATE OR REPLACE FUNCTION pot_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.pot_desc		= TRIM(NEW.pot_desc);
		NEW.pot_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.pot_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'pot_trigger') THEN
			CREATE TRIGGER pot_trigger
			BEFORE INSERT OR UPDATE ON poteri
			FOR EACH ROW
			EXECUTE PROCEDURE pot_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella cariche
*/
CREATE TABLE IF NOT EXISTS cariche
(
	car_codice			BIGSERIAL PRIMARY KEY,
	car_desc			VARCHAR (100) NOT NULL DEFAULT '',
	car_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	car_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	car_user			INTEGER
);
CREATE INDEX IF NOT EXISTS car_desc ON cariche (car_desc, car_codice);

CREATE OR REPLACE FUNCTION car_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.car_desc		= TRIM(NEW.car_desc);
		NEW.car_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.car_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'car_trigger') THEN
			CREATE TRIGGER car_trigger
			BEFORE INSERT OR UPDATE ON cariche
			FOR EACH ROW
			EXECUTE PROCEDURE car_trigger_function();
		END IF;
	END
$$;


/*
*	Creazione Tabella rappresentanti
*/
CREATE TABLE IF NOT EXISTS rappresentanti
(
	rap_codice 	    	BIGSERIAL PRIMARY KEY,
	rap_cli				BIGINT,
	rap_pot				BIGINT,
	rap_car				BIGINT,
	rap_cognome 		VARCHAR (50) NOT NULL DEFAULT '',
	rap_nome	 		VARCHAR (50) NOT NULL DEFAULT '',
	rap_desc			VARCHAR (101) NOT NULL DEFAULT '',
	rap_data_nascita	DATE,
	rap_luogo_nascita	VARCHAR (100) NOT NULL DEFAULT '',
	rap_prov_nascita	VARCHAR (2) NOT NULL DEFAULT '',
	rap_cap_nascita		VARCHAR (5) NOT NULL DEFAULT '',
	rap_codfis			VARCHAR (16) NOT NULL DEFAULT '',
	rap_esposto			SMALLINT NOT NULL DEFAULT 0,
	rap_quota			DOUBLE PRECISION NOT NULL DEFAULT 0,
	rap_scadenza		DATE,
	rap_fino_a_revoca	SMALLINT NOT NULL DEFAULT 0,
	rap_note			TEXT NOT NULL DEFAULT '',
	rap_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	rap_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	rap_user			INTEGER,
	CONSTRAINT rap_cli_fkey FOREIGN KEY (rap_cli) REFERENCES clienti (cli_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT rap_pot_fkey FOREIGN KEY (rap_pot) REFERENCES poteri (pot_codice) ON UPDATE CASCADE ON DELETE RESTRICT,
	CONSTRAINT rap_car_fkey FOREIGN KEY (rap_car) REFERENCES cariche (car_codice) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS rap_desc ON rappresentanti (rap_desc, rap_codice);
CREATE INDEX IF NOT EXISTS rap_cliente ON rappresentanti (rap_cli, rap_codice);
CREATE INDEX IF NOT EXISTS rap_poteri ON rappresentanti (rap_pot, rap_codice);
CREATE INDEX IF NOT EXISTS rap_cariche ON rappresentanti (rap_car, rap_codice);

CREATE OR REPLACE FUNCTION rap_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.rap_cognome			= TRIM(NEW.rap_cognome);
	    NEW.rap_nome			= TRIM(NEW.rap_nome);
	    NEW.rap_desc			= TRIM(CONCAT(TRIM(NEW.rap_cognome), ' ', TRIM(NEW.rap_nome)));
	    NEW.rap_luogo_nascita	= TRIM(NEW.rap_luogo_nascita);
	    NEW.rap_prov_nascita	= TRIM(NEW.rap_prov_nascita);
	    NEW.rap_cap_nascita		= TRIM(NEW.rap_cap_nascita);
	    NEW.rap_codfis			= UPPER(TRIM(NEW.rap_codfis));
	    NEW.rap_note			= TRIM(NEW.rap_note);
		NEW.rap_last_update	= NOW();
	    IF NEW.rap_car = 0 THEN
			NEW.rap_car = NULL;
		END IF;
	    IF NEW.rap_pot = 0 THEN
			NEW.rap_pot = NULL;
		END IF;
   	    IF  (TG_OP = 'INSERT') THEN
        	NEW.rap_created_at	= NOW();
        END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'rap_trigger') THEN
			CREATE TRIGGER rap_trigger
			BEFORE INSERT OR UPDATE ON rappresentanti
			FOR EACH ROW
			EXECUTE PROCEDURE rap_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella commercialisti
*/
CREATE TABLE IF NOT EXISTS commercialisti
(
	cmm_codice 	    	BIGSERIAL PRIMARY KEY,
	cmm_cli				BIGINT,
	cmm_cognome 		VARCHAR (50) NOT NULL DEFAULT '',
	cmm_nome	 		VARCHAR (50) NOT NULL DEFAULT '',
	cmm_desc			VARCHAR (101) NOT NULL DEFAULT '',
	cmm_citta			VARCHAR (100) NOT NULL DEFAULT '',
	cmm_indirizzo		VARCHAR (100) NOT NULL DEFAULT '',
	cmm_prov			VARCHAR (2) NOT NULL DEFAULT '',
	cmm_cap				VARCHAR (5) NOT NULL DEFAULT '',
	cmm_tel1			VARCHAR (15) NOT NULL DEFAULT '',
	cmm_tel3			VARCHAR (15) NOT NULL DEFAULT '',
	cmm_cell			VARCHAR (15) NOT NULL DEFAULT '',
	cmm_email 			VARCHAR(100) NOT NULL DEFAULT '',
	cmm_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	cmm_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	cmm_user			INTEGER,
	CONSTRAINT cmm_cli_fkey FOREIGN KEY (cmm_cli) REFERENCES clienti (cli_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS cmm_desc ON commercialisti (cmm_desc, cmm_codice);
CREATE INDEX IF NOT EXISTS cmm_cliente ON commercialisti (cmm_cli, cmm_codice);

CREATE OR REPLACE FUNCTION cmm_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.cmm_cognome			= TRIM(NEW.cmm_cognome);
	    NEW.cmm_nome			= TRIM(NEW.cmm_nome);
	    NEW.cmm_desc			= TRIM(CONCAT(TRIM(NEW.cmm_cognome), ' ', TRIM(NEW.cmm_nome)));
	    NEW.cmm_indirizzo		= TRIM(NEW.cmm_indirizzo);
	    NEW.cmm_citta			= TRIM(NEW.cmm_citta);
	    NEW.cmm_prov			= TRIM(NEW.cmm_prov);
	    NEW.cmm_cap				= TRIM(NEW.cmm_cap);
	    NEW.cmm_tel1			= TRIM(NEW.cmm_tel1);
	    NEW.cmm_tel2			= TRIM(NEW.cmm_tel2);
	    NEW.cmm_cell			= TRIM(NEW.cmm_cell);
		NEW.cmm_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
        	NEW.cmm_created_at	= NOW();
        END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'cmm_trigger') THEN
			CREATE TRIGGER cmm_trigger
			BEFORE INSERT OR UPDATE ON commercialisti
			FOR EACH ROW
			EXECUTE PROCEDURE cmm_trigger_function();
		END IF;
	END
$$;


















/*
	Tabella Scadenze Ditte
*/
CREATE TABLE IF NOT EXISTS scaditte
(
    scd_dit					INTEGER NOT NULL,
	scd_codice				INTEGER NOT NULL DEFAULT 1,
	scd_data				DATE,
	scd_desc				VARCHAR(512) NOT NULL DEFAULT '',
	scd_scad_alert_before	SMALLINT NOT NULL DEFAULT 0,
	scd_scad_alert_after	SMALLINT NOT NULL DEFAULT 0,
	scd_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	scd_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT scd_codice PRIMARY KEY (scd_dit, scd_codice),
	CONSTRAINT scd_ditte_fkey FOREIGN KEY (scd_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX scd_desc ON scaditte (scd_desc, scd_dit, scd_codice);

CREATE OR REPLACE FUNCTION scd_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.scd_desc		= TRIM(NEW.scd_desc);
		NEW.scd_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.scd_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'scd_trigger') THEN
			CREATE TRIGGER scd_trigger
			BEFORE INSERT OR UPDATE ON scaditte
			FOR EACH ROW
			EXECUTE PROCEDURE scd_trigger_function();
		END IF;
	END
$$;


/*
	Creazione Tabella check list
	Tipo 	: 	0 - Dipendenti
				1 - Cantieri
				2 - Ditte
				3 - Mezzi e Attrezzature

	Settore	: 	0 - Amministrazione
				1 - Autorizzazioni
				2 - Sicurezza
				3 - Tecnico
				4 - Unilav
				5 - Corsi
*/
CREATE TABLE IF NOT EXISTS checklist
(
    chk_codice			INTEGER NOT NULL DEFAULT 1 CHECK (chk_codice > 0),
	chk_tipo 		    SMALLINT NOT NULL DEFAULT 0,
	chk_settore 	    SMALLINT NOT NULL DEFAULT 0,
	chk_desc			VARCHAR(512),
	chk_default			SMALLINT NOT NULL DEFAULT 0,
	chk_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	chk_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT chk_codice PRIMARY KEY (chk_codice)
);
CREATE INDEX chk_desc ON checklist (chk_codice);

CREATE OR REPLACE FUNCTION chk_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.chk_desc	= TRIM(NEW.chk_desc);
		NEW.chk_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.chk_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'chk_trigger') THEN
			CREATE TRIGGER ckk_trigger
			BEFORE INSERT OR UPDATE ON checklist
			FOR EACH ROW
			EXECUTE PROCEDURE chk_trigger_function();
		END IF;
	END
$$;




/*
*	Creazione Tablla categorie
*/
CREATE TABLE IF NOT EXISTS categorie
(
    cat_dit				INTEGER NOT NULL,
	cat_codice 		    INTEGER NOT NULL DEFAULT 1,
	cat_desc 		    VARCHAR (50) NOT NULL DEFAULT '',
	cat_title 		    VARCHAR (512) NOT NULL DEFAULT '',
	cat_data            SMALLINT NOT NULL DEFAULT 0,
	cat_ore             SMALLINT NOT NULL DEFAULT 0,
    cat_corso           SMALLINT NOT NULL DEFAULT 0,
    cat_norma  	        SMALLINT NOT NULL DEFAULT 0,
	cat_data_rilascio   SMALLINT NOT NULL DEFAULT 0,
	cat_date_scadenza   SMALLINT NOT NULL DEFAULT 0,
	cat_custom          SMALLINT NOT NULL DEFAULT 0,
	cat_custom_name     VARCHAR (50) NOT NULL DEFAULT '',
	cat_created_at	TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	cat_last_update	TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT cat_codice PRIMARY KEY (cat_dit, cat_codice),
	CONSTRAINT cat_ditte_fkey FOREIGN KEY (cat_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX cat_desc ON categorie (cat_desc, cat_dit, cat_codice);

CREATE OR REPLACE FUNCTION cat_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.cat_desc     	= UPPER(TRIM(NEW.cat_desc));
		NEW.cat_title    	= TRIM(NEW.cat_title);
		NEW.cat_custom_name = TRIM(NEW.cat_custom_name);
		NEW.cat_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.cat_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'cat_trigger') THEN
			CREATE TRIGGER cat_trigger
			BEFORE INSERT OR UPDATE ON categorie
			FOR EACH ROW
			EXECUTE PROCEDURE cat_trigger_function();
		END IF;
	END
$$;

/*
	Creazione Tabella Tipologie Mezzi
*/
CREATE TABLE IF NOT EXISTS tipologie
(
	tip_codice 		    INTEGER NOT NULL DEFAULT 1 CHECK(tip_codice > 0),
	tip_desc			VARCHAR (50) NOT NULL DEFAULT '',
	tip_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	tip_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT tip_codice PRIMARY KEY (tip_codice)
);
CREATE INDEX tip_desc ON tipologie (tip_desc, tip_codice);

CREATE OR REPLACE FUNCTION tip_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.tip_desc		= TRIM(NEW.tip_desc);
		NEW.tip_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.tip_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'tip_trigger') THEN
			CREATE TRIGGER tip_trigger
			BEFORE INSERT OR UPDATE ON tipologie
			FOR EACH ROW
			EXECUTE PROCEDURE tip_trigger_function();
		END IF;
	END
$$;

/*
	Creazione Tabella Marchi
*/
CREATE TABLE IF NOT EXISTS marchi
(
	mar_codice 		    INTEGER NOT NULL DEFAULT 1 CHECK(mar_codice > 0),
	mar_desc			VARCHAR (50) NOT NULL DEFAULT '',
	mar_note			TEXT NOT NULL DEFAULT  '',
	mar_url				TEXT NOT NULL DEFAULT  '',
	mar_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	mar_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT mar_codice PRIMARY KEY (mar_codice)
);
CREATE INDEX IF NOT EXISTS mar_desc ON marchi (mar_desc, mar_codice);

CREATE OR REPLACE FUNCTION mar_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.mar_desc		= TRIM(NEW.mar_desc);
	    NEW.mar_note		= TRIM(NEW.mar_note);
	    NEW.mar_url			= TRIM(NEW.mar_url);
		NEW.mar_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.mar_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'mar_trigger') THEN
			CREATE TRIGGER mar_trigger
			BEFORE INSERT OR UPDATE ON marchi
			FOR EACH ROW
			EXECUTE PROCEDURE mar_trigger_function();
		END IF;
	END
$$;

/*
	Tabella Immagini Marchi
*/
CREATE TABLE IF NOT EXISTS imgmarchi
(
	img_dit			INTEGER NOT NULL DEFAULT 0,
	img_codice		INTEGER NOT NULL,
	img_formato		SMALLINT NOT NULL DEFAULT 0,
	img_tipo		SMALLINT NOT NULL DEFAULT 0,
	img_bytes_size	INTEGER NOT NULL DEFAULT 0,
	img_created_at	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_last_update	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_data		BYTEA,
	CONSTRAINT img_mar_codice PRIMARY KEY (img_dit, img_codice, img_formato),
	CONSTRAINT img_mar_fkey FOREIGN KEY (img_codice) REFERENCES marchi (mar_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS img_marchi ON imgmarchi (img_codice);

CREATE OR REPLACE FUNCTION img_mar_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.img_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.img_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'img_mar_trigger') THEN
			CREATE TRIGGER img_mar_trigger
			BEFORE INSERT OR UPDATE ON imgmarchi
			FOR EACH ROW
			EXECUTE PROCEDURE img_mar_trigger_function();
		END IF;
	END
$$;



/*
	Creazione Tabella verifiche ALL V DL.gs 81/2008
*/
CREATE TABLE IF NOT EXISTS verifiche
(
	ver_codice 		    	INTEGER NOT NULL DEFAULT 1 CHECK(ver_codice > 0),
	ver_desc				VARCHAR (512) NOT NULL DEFAULT '',
	ver_funzionamento_anni	SMALLINT NOT NULL DEFAULT 0,
	ver_integrita_anni		SMALLINT NOT NULL DEFAULT 0,
	ver_interna_anni		SMALLINT NOT NULL DEFAULT 0,
	ver_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	ver_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT ver_codice PRIMARY KEY (ver_codice)
);
CREATE INDEX ver_desc ON verifiche (ver_desc,ver_codice);

CREATE OR REPLACE FUNCTION ver_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.ver_desc		= TRIM(NEW.ver_desc);
		NEW.ver_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.ver_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'ver_trigger') THEN
			CREATE TRIGGER ver_trigger
			BEFORE INSERT OR UPDATE ON verifiche
			FOR EACH ROW
			EXECUTE PROCEDURE ver_trigger_function();
		END IF;
	END
$$;

/*
	Creazione Tabella Modelli
*/
CREATE TABLE IF NOT EXISTS modelli
(
    mod_dit					INTEGER NOT NULL CHECK(mod_dit > 0),
	mod_codice 		    	INTEGER NOT NULL DEFAULT 1 CHECK(mod_codice > 0),
	mod_desc				VARCHAR (100) NOT NULL DEFAULT '',
	mod_cod_for				VARCHAR (25) NOT NULL  DEFAULT 0,
	mod_ver					INTEGER,
	mod_mar					INTEGER NOT NULL,
	mod_tip					INTEGER,
	mod_note				TEXT NOT NULL DEFAULT '',
	mod_verificato			SMALLINT NOT NULL DEFAULT 0,
	mod_user				INTEGER NOT NULL DEFAULT 0,
	mod_manuale_uso 		SMALLINT NOT NULL DEFAULT 0,
	mod_marchio_ce 			SMALLINT NOT NULL DEFAULT 0,
	mod_rispondenza_all_v 	SMALLINT NOT NULL DEFAULT 0,
	mod_formazione	 		SMALLINT NOT NULL DEFAULT 0,
	mod_corso			 	SMALLINT NOT NULL DEFAULT 0,
	mod_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	mod_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT mod_codice PRIMARY KEY (mod_dit, mod_codice),
	CONSTRAINT mod_ditte_fkey FOREIGN KEY (mod_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT mod_verifiche_fkey FOREIGN KEY (mod_ver) REFERENCES verifiche (ver_codice) ON UPDATE CASCADE ON DELETE RESTRICT,
	CONSTRAINT mod_marchi_fkey FOREIGN KEY (mod_mar) REFERENCES marchi (mar_codice) ON UPDATE CASCADE ON DELETE RESTRICT,
	CONSTRAINT mod_tipologie_fkey FOREIGN KEY (mod_tip) REFERENCES tipologie (tip_codice) ON UPDATE CASCADE ON DELETE RESTRICT
);

ALTER TABLE modelli
	ADD COLUMN mod_manuale_uso 		SMALLINT NOT NULL DEFAULT 1,
	ADD COLUMN mod_marchio_ce 			SMALLINT NOT NULL DEFAULT 1,
	ADD COLUMN mod_rispondenza_all_v 	SMALLINT NOT NULL DEFAULT 1,
	ADD COLUMN mod_formazione	 		SMALLINT NOT NULL DEFAULT 1,
	ADD COLUMN mod_corso			 	SMALLINT NOT NULL DEFAULT 1;


CREATE INDEX IF NOT EXISTS mod_desc ON modelli (mod_desc, mod_dit, mod_codice);
CREATE INDEX IF NOT EXISTS mod_ver ON modelli (mod_ver, mod_codice);
CREATE INDEX IF NOT EXISTS mod_mar ON modelli (mod_mar, mod_codice);
CREATE INDEX IF NOT EXISTS mod_tip ON modelli (mod_tip, mod_codice);

CREATE OR REPLACE FUNCTION mod_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.mod_desc		= TRIM(NEW.mod_desc);
		NEW.mod_last_update	= NOW();
	    IF NEW.mod_ver = 0 THEN
			NEW.mod_ver = NULL;
		END IF;
   	    IF NEW.mod_tip = 0 THEN
			NEW.mod_tip = NULL;
		END IF;
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.mod_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'mod_trigger') THEN
			CREATE TRIGGER mod_trigger
			BEFORE INSERT OR UPDATE ON modelli
			FOR EACH ROW
			EXECUTE PROCEDURE mod_trigger_function();
		END IF;
	END
$$;



/*
	Creazione Tabella Mezzi
*/
CREATE TABLE IF NOT EXISTS mezzi
(
    mez_dit					INTEGER NOT NULL,
	mez_codice 		    	INTEGER NOT NULL DEFAULT 1,
	mez_dit_mod				INTEGER NOT NULL,
	mez_mod 		    	INTEGER NOT NULL,
	mez_desc				VARCHAR(512) NOT NULL DEFAULT '',
	mez_note				TEXT NOT NULL DEFAULT '',
	mez_data_imm			DATE,
	mez_data_acq			DATE,
	mez_data_dis			DATE,
	mez_serial				VARCHAR(25) NOT NULL DEFAULT '',
	mez_targa				VARCHAR(25) NOT NULL DEFAULT '',
	mez_gps					VARCHAR(25) NOT NULL DEFAULT '',
	mez_telaio				VARCHAR(50) NOT NULL DEFAULT '',
	mez_proprieta	   		SMALLINT NOT NULL DEFAULT 1,
	mez_type		   		SMALLINT NOT NULL DEFAULT 0,
	mez_manuale_uso 		SMALLINT NOT NULL DEFAULT 0,
	mez_marchio_ce 			SMALLINT NOT NULL DEFAULT 0,
	mez_rispondenza_all_v 	SMALLINT NOT NULL DEFAULT 0,
	mez_formazione	 		SMALLINT NOT NULL DEFAULT 0,
	mez_corso			 	SMALLINT NOT NULL DEFAULT 0,
	mez_cod_for				VARCHAR(25) NOT NULL DEFAULT '',
	mez_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	mez_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT mez_codice PRIMARY KEY (mez_dit, mez_codice),
	CONSTRAINT mez_ditte_fkey FOREIGN KEY (mez_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT mez_ditta_mod_fkey FOREIGN KEY (mez_dit_mod) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE RESTRICT,
	CONSTRAINT mez_modello_fkey FOREIGN KEY (mez_dit_mod, mez_mod) REFERENCES modelli (mod_dit, mod_codice) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX mez_desc ON mezzi (mez_desc, mez_dit, mez_codice);
CREATE INDEX mez_ditta_mod ON mezzi (mez_dit_mod, mez_mod, mez_codice);

CREATE OR REPLACE FUNCTION mez_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.mez_desc		= TRIM(NEW.mez_desc);
	    NEW.mez_note		= TRIM(NEW.mez_note);
	    NEW.mez_serial		= TRIM(NEW.mez_serial);
	    NEW.mez_targa		= UPPER(TRIM(NEW.mez_targa));
	    NEW.mez_gps			= TRIM(NEW.mez_gps);
	    NEW.mez_telaio		= TRIM(NEW.mez_telaio);
		NEW.mez_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.mez_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'mez_trigger') THEN
			CREATE TRIGGER mez_trigger
			BEFORE INSERT OR UPDATE ON mezzi
			FOR EACH ROW
			EXECUTE PROCEDURE mez_trigger_function();
		END IF;
	END
$$;

/*
	Tabella Video Mezzi
*/
CREATE TABLE IF NOT EXISTS videomezzi
(
    vme_dit					INTEGER NOT NULL,
	vme_codice				INTEGER NOT NULL DEFAULT 1,
	vme_mez					INTEGER NOT NULL,
	vme_livello				SMALLINT NOT NULL,
	vme_data				DATE,
	vme_desc				VARCHAR(512) NOT NULL DEFAULT '',
	vme_url					TEXT NOT NULL DEFAULT '',
	vme_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	vme_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT vme_codice PRIMARY KEY (vme_dit, vme_codice),
	CONSTRAINT vme_ditte_fkey FOREIGN KEY (vme_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT vme_mezzi_fkey FOREIGN KEY (vme_dit, vme_mez) REFERENCES mezzi (mez_dit, mez_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX vme_desc ON videomezzi (vme_desc, vme_dit, vme_codice);
CREATE INDEX vme_mezzi ON videomezzi (vme_dit, vme_mez, vme_livello);
CREATE INDEX vme_type ON videomezzi (vme_dit, vme_livello, vme_mez);

CREATE OR REPLACE FUNCTION vme_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.vme_desc		= TRIM(NEW.vme_desc);
		NEW.vme_url			= TRIM(NEW.vme_url);
		NEW.vme_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.vme_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'vme_trigger') THEN
			CREATE TRIGGER vme_trigger
			BEFORE INSERT OR UPDATE ON videomezzi
			FOR EACH ROW
			EXECUTE PROCEDURE vme_trigger_function();
		END IF;
	END
$$;


/*
*	Creazione Tabella mansioni
*/
CREATE TABLE IF NOT EXISTS mansioni
(
	man_codice 		    INTEGER NOT NULL DEFAULT 1 CHECK(man_codice > 0),
	man_desc			VARCHAR (100) NOT NULL DEFAULT '',
	man_rischio			SMALLINT NOT NULL DEFAULT 0,
	man_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	man_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	man_user			INTEGER NOT NULL DEFAULT 0,
	CONSTRAINT man_codice PRIMARY KEY (man_codice)
);
CREATE INDEX man_desc ON mansioni (man_desc, man_codice);

CREATE OR REPLACE FUNCTION man_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.man_desc		= TRIM(NEW.man_desc);
		NEW.man_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.man_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'man_trigger') THEN
			CREATE TRIGGER man_trigger
			BEFORE INSERT OR UPDATE ON mansioni
			FOR EACH ROW
			EXECUTE PROCEDURE man_trigger_function();
		END IF;
	END
$$;

/*
*	Tabella Dipendenti
*/
CREATE TABLE IF NOT EXISTS dipendenti
(
	dip_dit					INTEGER NOT NULL,
    dip_codice				INTEGER NOT NULL DEFAULT 1,
	dip_cognome				VARCHAR(50),
	dip_nome				VARCHAR(50),
	dip_desc				VARCHAR (101) NOT NULL DEFAULT '',
	dip_indirizzo			VARCHAR (100) NOT NULL DEFAULT '',
	dip_citta				VARCHAR (50) NOT NULL DEFAULT '',
	dip_cap					VARCHAR (5) NOT NULL DEFAULT '',
	dip_prov				VARCHAR (5) NOT NULL DEFAULT '',
	dip_citta_nas			VARCHAR (50) NOT NULL DEFAULT '',
	dip_cap_nas				VARCHAR (5) NOT NULL DEFAULT '',
	dip_prov_nas			VARCHAR (5) NOT NULL DEFAULT '',
	dip_data_nas			DATE NOT NULL,
	dip_codfis				VARCHAR (16) NOT NULL DEFAULT '',
	dip_tel					VARCHAR (15) NOT NULL DEFAULT '',
	dip_cell1				VARCHAR (15) NOT NULL DEFAULT '',
	dip_cell2				VARCHAR (15) NOT NULL DEFAULT '',
	dip_email				VARCHAR (100) NOT NULL DEFAULT '',
	dip_pec					VARCHAR (100) NOT NULL DEFAULT '',
	dip_data_assunzione		DATE,
	dip_data_fine_rapporto	DATE,
	dip_scad_permesso_sog	DATE,
	dip_note 				TEXT NOT NULL DEFAULT '',
	dip_categoria			SMALLINT NOT NULL DEFAULT 0,
   	dip_inquadramento		VARCHAR(100) NOT NULL DEFAULT '',
	dip_deleted				SMALLINT NOT NULL DEFAULT 0,
	dip_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	dip_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	dip_user				INTEGER NOT NULL DEFAULT 0,
	CONSTRAINT dip_codice PRIMARY KEY (dip_dit, dip_codice),
	CONSTRAINT dip_ditte_fkey FOREIGN KEY (dip_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE RESTRICT
);

ALTER TABLE dipendenti
	ADD COLUMN dip_categoria		SMALLINT NOT NULL DEFAULT 0,
    ADD COLUMN dip_inquadramento	VARCHAR(100) NOT NULL DEFAULT '',
    ADD COLUMN dip_mansioni			TEXT NOT NULL DEFAULT  '';

ALTER TABLE dipendenti
	ADD COLUMN IF NOT EXISTS dip_tipo_contratto	SMALLINT NOT NULL DEFAULT 0;
ALTER TABLE dipendenti
	ADD COLUMN IF NOT EXISTS dip_user	INTEGER NOT NULL DEFAULT 0;

CREATE INDEX dip_desc ON dipendenti (dip_desc, dip_dit, dip_codice);
CREATE INDEX dip_tel ON dipendenti (dip_tel, dip_dit, dip_codice);
CREATE INDEX dip_cell1 ON dipendenti (dip_cell1, dip_dit, dip_codice);
CREATE INDEX dip_cell2 ON dipendenti (dip_cell2, dip_dit, dip_codice);
CREATE INDEX dip_email ON dipendenti (dip_email, dip_dit, dip_codice);
CREATE INDEX dip_pec ON dipendenti (dip_pec, dip_dit, dip_codice);

CREATE OR REPLACE FUNCTION dip_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.dip_desc		= TRIM(NEW.dip_desc);
		NEW.dip_cognome		= TRIM(NEW.dip_cognome);
		NEW.dip_nome		= TRIM(NEW.dip_nome);
		NEW.dip_desc		= TRIM(NEW.dip_desc);
		NEW.dip_indirizzo	= TRIM(NEW.dip_indirizzo);
		NEW.dip_citta		= TRIM(NEW.dip_citta);
		NEW.dip_cap			= TRIM(NEW.dip_cap);
		NEW.dip_prov		= TRIM(NEW.dip_prov);
		NEW.dip_citta_nas	= TRIM(NEW.dip_citta_nas);
		NEW.dip_cap_nas		= TRIM(NEW.dip_cap_nas);
		NEW.dip_prov_nas	= TRIM(NEW.dip_prov_nas);
		NEW.dip_codfis		= UPPER(TRIM(NEW.dip_codfis));
		NEW.dip_tel			= TRIM(NEW.dip_tel);
		NEW.dip_cell1		= TRIM(NEW.dip_cell1);
		NEW.dip_cell2		= TRIM(NEW.dip_cell2);
		NEW.dip_email		= LOWER(TRIM(NEW.dip_email));
		NEW.dip_pec			= LOWER(TRIM(NEW.dip_pec));
		NEW.dip_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.dip_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'dip_trigger') THEN
			CREATE TRIGGER dip_trigger
			BEFORE INSERT OR UPDATE ON dipendenti
			FOR EACH ROW
			EXECUTE PROCEDURE dip_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella mansioni dipendenti
*/
CREATE TABLE IF NOT EXISTS dipmansioni
(
    dma_dit				INTEGER NOT NULL,
	dma_dip 		    INTEGER NOT NULL,
	dma_man				INTEGER NOT NULL,
	dma_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	dma_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	dma_user			INTEGER NOT NULL DEFAULT 0,
	CONSTRAINT dma_codice PRIMARY KEY (dma_dit, dma_dip, dma_man),
	CONSTRAINT dma_ditte_fkey FOREIGN KEY (dma_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT dma_dipendenti_fkey FOREIGN KEY (dma_dit, dma_dip) REFERENCES dipendenti (dip_dit, dip_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT dma_mansioni_fkey FOREIGN KEY (dma_man) REFERENCES mansioni (man_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS dma_ditte ON dipmansioni (dma_dit);
CREATE INDEX IF NOT EXISTS dma_dipendenti ON dipmansioni (dma_dit, dma_dip);
CREATE INDEX IF NOT EXISTS dma_mansioni ON dipmansioni (dma_man);

CREATE OR REPLACE FUNCTION dma_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.dma_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.dma_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'dma_trigger') THEN
			CREATE TRIGGER dma_trigger
			BEFORE INSERT OR UPDATE ON dipmansioni
			FOR EACH ROW
			EXECUTE PROCEDURE dma_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella sedi dipendenti
*/
CREATE TABLE IF NOT EXISTS dipsedi
(
    dse_dit				INTEGER NOT NULL,
	dse_dip 		    INTEGER NOT NULL,
	dse_sed				INTEGER NOT NULL,
	dse_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	dse_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	dse_user			INTEGER NOT NULL DEFAULT 0,
	CONSTRAINT dse_codice PRIMARY KEY (dse_dit, dse_dip, dse_sed),
	CONSTRAINT dse_ditte_fkey FOREIGN KEY (dse_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT dse_dipendenti_fkey FOREIGN KEY (dse_dit, dse_dip) REFERENCES dipendenti (dip_dit, dip_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT dse_sedi_fkey FOREIGN KEY (dse_dit, dse_sed) REFERENCES sedditte (sed_dit, sed_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS dse_ditte ON dipsedi (dse_dit);
CREATE INDEX IF NOT EXISTS dse_dipendenti ON dipsedi (dse_dit, dse_dip);
CREATE INDEX IF NOT EXISTS dse_sedi ON dipsedi (dse_dit, dse_sed);

CREATE OR REPLACE FUNCTION dse_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.dse_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.dse_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'dse_trigger') THEN
			CREATE TRIGGER dse_trigger
			BEFORE INSERT OR UPDATE ON dipsedi
			FOR EACH ROW
			EXECUTE PROCEDURE dse_trigger_function();
		END IF;
	END
$$;



/*
*	Creazione Tabella visite mediche dipendenti
*/
CREATE TABLE IF NOT EXISTS dipvisite
(
    dvi_dit				INTEGER NOT NULL,
    dvi_codice			INTEGER NOT NULL DEFAULT 1,
	dvi_dip 		    INTEGER NOT NULL,
	dvi_medico			VARCHAR(100) NOT NULL DEFAULT '',
	dvi_data			DATE NOT NULL,
	dvi_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	dvi_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	dvi_user			INTEGER NOT NULL DEFAULT 0,
	CONSTRAINT dvi_codice PRIMARY KEY (dvi_dit, dvi_codice),
	CONSTRAINT dvi_ditte_fkey FOREIGN KEY (dvi_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT dvi_dipendenti_fkey FOREIGN KEY (dvi_dit, dvi_dip) REFERENCES dipendenti (dip_dit, dip_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS dvi_ditte ON dipvisite (dvi_dit);
CREATE INDEX IF NOT EXISTS dvi_dipendenti ON dipvisite (dvi_dit, dvi_dip);

CREATE OR REPLACE FUNCTION dvi_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.dvi_medico = TRIM(NEW.dvi_medico);
		NEW.dvi_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.dvi_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'dvi_trigger') THEN
			CREATE TRIGGER dvi_trigger
			BEFORE INSERT OR UPDATE ON dipvisite
			FOR EACH ROW
			EXECUTE PROCEDURE dvi_trigger_function();
		END IF;
	END
$$;


/*
	Tabella Immagini Dipendenti
*/
CREATE TABLE IF NOT EXISTS imgdipendenti
(
	img_dit			INTEGER NOT NULL,
	img_codice		INTEGER NOT NULL,
	img_formato		SMALLINT NOT NULL DEFAULT 0,
	img_tipo		SMALLINT NOT NULL DEFAULT 0,
	img_bytes_size	INTEGER NOT NULL DEFAULT 0,
	img_created_at	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_last_update	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_data		BYTEA,
	CONSTRAINT img_dip_codice PRIMARY KEY (img_dit, img_codice, img_formato),
	CONSTRAINT img_dip_ditte_fkey FOREIGN KEY (img_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT img_dipendenti_fkey FOREIGN KEY (img_dit, img_codice) REFERENCES dipendenti (dip_dit, dip_codice) ON UPDATE CASCADE ON DELETE CASCADE
)  PARTITION BY LIST (img_dit);

CREATE INDEX IF NOT EXISTS img_dip_ditte ON imgdipendenti (img_dit);
CREATE INDEX IF NOT EXISTS img_dipendenti ON imgdipendenti (img_dit, img_codice);

CREATE OR REPLACE FUNCTION img_dip_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.img_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.img_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'img_dip_trigger') THEN
			CREATE TRIGGER img_dip_trigger
			BEFORE INSERT OR UPDATE ON imgdipendenti
			FOR EACH ROW
			EXECUTE PROCEDURE img_dip_trigger_function();
		END IF;
	END
$$;

/*
	Tabella Scadenze Dipendenti
*/
CREATE TABLE IF NOT EXISTS scadipendenti
(
    scp_dit					INTEGER NOT NULL,
	scp_codice				INTEGER NOT NULL DEFAULT 1,
	scp_dip					INTEGER NOT NULL,
	scp_data				DATE,
	scp_desc				VARCHAR(512) NOT NULL DEFAULT '',
	scp_scad_alert_before	SMALLINT NOT NULL DEFAULT 0,
	scp_scad_alert_after	SMALLINT NOT NULL DEFAULT 0,
	scp_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	scp_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT scp_codice PRIMARY KEY (scp_dit, scp_codice),
	CONSTRAINT scp_ditte_fkey FOREIGN KEY (scp_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT scp_dipendenti_fkey FOREIGN KEY (scp_dit, scp_dip) REFERENCES dipendenti (dip_dit, dip_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX scp_desc ON scadipendenti (scp_desc, scp_dit, scp_codice);
CREATE INDEX scp_dipendenti ON scadipendenti (scp_dit, scp_dip, scp_data, scp_codice);

CREATE OR REPLACE FUNCTION scp_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.scp_desc		= TRIM(NEW.scp_desc);
		NEW.scp_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.scp_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'scp_trigger') THEN
			CREATE TRIGGER scp_trigger
			BEFORE INSERT OR UPDATE ON scadipendenti
			FOR EACH ROW
			EXECUTE PROCEDURE scp_trigger_function();
		END IF;
	END
$$;

/*
*	Tabella Cantieri
*/
CREATE TABLE IF NOT EXISTS cantieri
(
	can_dit					INTEGER NOT NULL,
    can_codice				INTEGER NOT NULL DEFAULT 1,
	can_desc				VARCHAR (512),
	can_indirizzo			VARCHAR (100) NOT NULL DEFAULT '',
	can_citta				VARCHAR (50) NOT NULL DEFAULT '',
	can_cap					VARCHAR (5) NOT NULL DEFAULT '',
	can_prov				VARCHAR (5) NOT NULL DEFAULT '',
	can_data_inizio			DATE,
	can_data_fine			DATE,
	can_note 				TEXT NOT NULL DEFAULT '',

	can_approvazione_progetto_esecutivo	VARCHAR(256) NOT NULL DEFAULT '',
	can_ente_appaltante					VARCHAR(256) NOT NULL DEFAULT '',
	can_ufficio_competente				VARCHAR(256) NOT NULL DEFAULT '',
	can_rup								VARCHAR(256) NOT NULL DEFAULT '',
	can_progettazione_esecutiva			VARCHAR(256) NOT NULL DEFAULT '',
	can_direttore_lavori				VARCHAR(256) NOT NULL DEFAULT '',
	can_coord_sicurezza_progettazione	VARCHAR(256) NOT NULL DEFAULT '',
	can_coord_sicurezza_esecutiva		VARCHAR(256) NOT NULL DEFAULT '',
	can_importo_finanziamento			DOUBLE PRECISION NOT NULL DEFAULT 0,
	can_importo_lavori					DOUBLE PRECISION NOT NULL DEFAULT 0,
	can_importo_base_asta				DOUBLE PRECISION NOT NULL DEFAULT 0,
	can_oneri_sicurezza					DOUBLE PRECISION NOT NULL DEFAULT 0,
	can_importo_contrattuale			DOUBLE PRECISION NOT NULL DEFAULT 0,
	can_estremi_contratto				VARCHAR(256) NOT NULL DEFAULT '',
	can_notifica_preliminare			VARCHAR(256) NOT NULL DEFAULT '',
	can_direttore_tecnico				VARCHAR(256) NOT NULL DEFAULT '',
	can_responsabile_cantiere			VARCHAR(256) NOT NULL DEFAULT '',
	can_rspp							VARCHAR(256) NOT NULL DEFAULT '',
	can_durata_lavori					INTEGER NOT NULL DEFAULT 0,
	can_imprese_subappaltatrici			TEXT NOT NULL DEFAULT '',
	can_direttore_operativo				VARCHAR(256) NOT NULL DEFAULT '',
	can_ispettore_di_cantiere			VARCHAR(256) NOT NULL DEFAULT '',
	can_collaudo_statico				VARCHAR(256) NOT NULL DEFAULT '',
	can_collaudo_tecnico_amministrativo	VARCHAR(256) NOT NULL DEFAULT '',
	can_impresa_aggiudicataria			VARCHAR(256) NOT NULL DEFAULT '',
	can_cup								VARCHAR(15) NOT NULL DEFAULT '',
	can_cig								VARCHAR(10) NOT NULL DEFAULT '',
	can_latitudine						DOUBLE PRECISION NOT NULL DEFAULT 0,
	can_longitudine						DOUBLE PRECISION NOT NULL DEFAULT 0,
	can_subappalto 						SMALLINT NOT NULL DEFAULT 0,
	can_deleted							SMALLINT NOT NULL DEFAULT 0,
	can_created_at						TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	can_last_update						TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT can_codice PRIMARY KEY (can_dit, can_codice),
	CONSTRAINT can_ditte_fkey FOREIGN KEY (can_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE INDEX can_descriz ON cantieri (can_desc, can_dit, can_codice);

ALTER TABLE cantieri
ADD COLUMN IF NOT EXISTS can_subappalto SMALLINT NOT NULL DEFAULT 0;



CREATE OR REPLACE FUNCTION can_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.can_desc		= TRIM(NEW.can_desc);
		NEW.can_indirizzo	= TRIM(NEW.can_indirizzo);
		NEW.can_citta		= TRIM(NEW.can_citta);
		NEW.can_cap			= TRIM(NEW.can_cap);
		NEW.can_prov		= TRIM(NEW.can_prov);
		NEW.can_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.can_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'can_trigger') THEN
			CREATE TRIGGER can_trigger
			BEFORE INSERT OR UPDATE ON cantieri
			FOR EACH ROW
			EXECUTE PROCEDURE can_trigger_function();
		END IF;
	END
$$;



/*
	Tabella Utenti Cantieri

	La taballe contiene gli utenti speciali (Committente, CSE e Direttori lavori) che devono aver accesso ai dati del cantiere
*/
CREATE TABLE IF NOT EXISTS usrcantieri
(
	usc_codice		BIGSERIAL PRIMARY KEY,
	usc_dit			INTEGER NOT NULL,
	usc_can			INTEGER NOT NULL,
	usc_tipo		SMALLINT NOT NULL DEFAULT 0,
	usc_rag_soc1	VARCHAR(50) NOT NULL DEFAULT '',
	usc_rag_soc2	VARCHAR(50) NOT NULL DEFAULT '',
	usc_desc		VARCHAR(101) NOT NULL DEFAULT '',
	usc_email		TEXT NOT NULL DEFAULT '',
	usc_pec		TEXT NOT NULL DEFAULT '',
	usc_created_at	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	usc_last_update	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	CONSTRAINT usc_ditte_fkey FOREIGN KEY (usc_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT usc_cantieri_fkey FOREIGN KEY (usc_dit, usc_can) REFERENCES cantieri (can_dit, can_codice) ON UPDATE CASCADE ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS usc_ditte ON usrcantieri (usc_dit, usc_codice);
CREATE INDEX IF NOT EXISTS usc_cantieri ON usrcantieri (usc_dit, usc_can, usc_codice);
CREATE INDEX IF NOT EXISTS usc_desc ON usrcantieri (usc_dit, usc_can, usc_desc, usc_codice);

CREATE OR REPLACE FUNCTION usc_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.usc_rag_soc1 = TRIM(NEW.usc_rag_soc1);
	    NEW.usc_rag_soc2 = TRIM(NEW.usc_rag_soc2);
	    NEW.usc_desc = TRIM(CONCAT(NEW.usc_rag_soc1, ' ', NEW.usc_rag_soc1));
	    NEW.usc_email = LOWER(TRIM(NEW.usc_email));
		NEW.usc_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.usc_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'usc_trigger') THEN
			CREATE TRIGGER usc_trigger
			BEFORE INSERT OR UPDATE ON usrcantieri
			FOR EACH ROW
			EXECUTE PROCEDURE usc_trigger_function();
		END IF;
	END
$$;


/*
	Tabella Immagini Cantieri
*/
CREATE TABLE IF NOT EXISTS imgcantieri
(
	img_dit			INTEGER NOT NULL,
	img_codice		INTEGER NOT NULL,
	img_formato		SMALLINT NOT NULL DEFAULT 0,
	img_tipo		SMALLINT NOT NULL DEFAULT 0,
	img_bytes_size	INTEGER NOT NULL DEFAULT 0,
	img_created_at	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_last_update	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_data		BYTEA,
	CONSTRAINT img_can_codice PRIMARY KEY (img_dit, img_codice, img_formato),
	CONSTRAINT img_can_ditte_fkey FOREIGN KEY (img_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT img_cantieri_fkey FOREIGN KEY (img_dit, img_codice) REFERENCES cantieri (can_dit, can_codice) ON UPDATE CASCADE ON DELETE CASCADE
)  PARTITION BY LIST (img_dit);

CREATE INDEX IF NOT EXISTS img_can_ditte ON imgcantieri (img_dit);
CREATE INDEX IF NOT EXISTS img_cantieri ON imgcantieri (img_dit, img_codice);

CREATE OR REPLACE FUNCTION img_can_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.img_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.img_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'img_can_trigger') THEN
			CREATE TRIGGER img_can_trigger
			BEFORE INSERT OR UPDATE ON imgcantieri
			FOR EACH ROW
			EXECUTE PROCEDURE img_can_trigger_function();
		END IF;
	END
$$;

/*
	Tabella Scadenze Cantieri
*/
CREATE TABLE IF NOT EXISTS scacantieri
(
    scc_dit					INTEGER NOT NULL,
	scc_codice				INTEGER NOT NULL DEFAULT 1,
	scc_can					INTEGER NOT NULL,
	scc_data				DATE,
	scc_desc				VARCHAR(512) NOT NULL DEFAULT '',
	scc_scad_alert_before	SMALLINT NOT NULL DEFAULT 0,
	scc_scad_alert_after	SMALLINT NOT NULL DEFAULT 0,
	scc_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	scc_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT scc_codice PRIMARY KEY (scc_dit, scc_codice),
	CONSTRAINT scc_ditte_fkey FOREIGN KEY (scc_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT scc_cantieri_fkey FOREIGN KEY (scc_dit, scc_can) REFERENCES cantieri (can_dit, can_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX scc_desc ON scacantieri (scc_desc, scc_dit, scc_codice);
CREATE INDEX scc_cantieri ON scacantieri (scc_dit, scc_can, scc_data, scc_codice);

CREATE OR REPLACE FUNCTION scc_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.scc_desc		= TRIM(NEW.scc_desc);
		NEW.scc_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.scc_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'scc_trigger') THEN
			CREATE TRIGGER scc_trigger
			BEFORE INSERT OR UPDATE ON scacantieri
			FOR EACH ROW
			EXECUTE PROCEDURE scc_trigger_function();
		END IF;
	END
$$;

/*
	Tabella Dipendenti Cantieri
*/
CREATE TABLE IF NOT EXISTS dipcantieri
(
    dic_dit					INTEGER NOT NULL,
	dic_can					INTEGER NOT NULL,
	dic_dip					INTEGER NOT NULL,
	dic_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	dic_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT dic_codice PRIMARY KEY (dic_dit, dic_can, dic_dip),
	CONSTRAINT dic_ditte_fkey FOREIGN KEY (dic_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT dic_cantieri_fkey FOREIGN KEY (dic_dit, dic_can) REFERENCES cantieri (can_dit, can_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT dic_dipendenti_fkey FOREIGN KEY (dic_dit, dic_dip) REFERENCES dipendenti (dip_dit, dip_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS dic_cantieri ON dipcantieri (dic_dit, dic_can);
CREATE INDEX IF NOT EXISTS dic_dipendenti ON dipcantieri (dic_dit, dic_dip);
CREATE INDEX IF NOT EXISTS dic_ditte ON dipcantieri (dic_dit);

CREATE OR REPLACE FUNCTION dic_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.dic_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.dic_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'dic_trigger') THEN
			CREATE TRIGGER dic_trigger
			BEFORE INSERT OR UPDATE ON dipcantieri
			FOR EACH ROW
			EXECUTE PROCEDURE dic_trigger_function();
		END IF;
	END
$$;


/*
	Tabella Mezzi Cantieri
*/
CREATE TABLE IF NOT EXISTS mezcantieri
(
    mec_dit					INTEGER NOT NULL,
	mec_can					INTEGER NOT NULL,
	mec_mez					INTEGER NOT NULL,
	mec_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	mec_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT mec_codice PRIMARY KEY (mec_dit, mec_can, mec_mez),
	CONSTRAINT mec_ditte_fkey FOREIGN KEY (mec_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT mec_cantieri_fkey FOREIGN KEY (mec_dit, mec_can) REFERENCES cantieri (can_dit, can_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT mec_mezzi_fkey FOREIGN KEY (mec_dit, mec_mez) REFERENCES mezzi (mez_dit, mez_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS mec_cantieri ON mezcantieri (mec_dit, mec_can);
CREATE INDEX IF NOT EXISTS mec_mezzi ON mezcantieri (mec_dit, mec_mez);
CREATE INDEX IF NOT EXISTS mec_ditte ON mezcantieri (mec_dit);

CREATE OR REPLACE FUNCTION mec_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.mec_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.mec_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'mec_trigger') THEN
			CREATE TRIGGER mec_trigger
			BEFORE INSERT OR UPDATE ON mezcantieri
			FOR EACH ROW
			EXECUTE PROCEDURE mec_trigger_function();
		END IF;
	END
$$;


/*
	Tabella Subappalti Cantieri
	sub_dit_app : Ditta appaltante
	sub_dit_can : Cantiere ditta appaltante
	sub_dit_sub	: Ditta subappaltatrice
	sub_dit_can	: Cantiere ditta subappaltatrice
*/
CREATE TABLE IF NOT EXISTS subappalti
(
    sub_codice 				BIGSERIAL PRIMARY KEY,
    sub_dit_app				INTEGER NOT NULL,
    sub_can_app				INTEGER NOT NULL,
    sub_dit_sub				INTEGER NOT NULL,
	sub_can_sub				INTEGER NOT NULL,
	sub_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	sub_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT sub_key UNIQUE (sub_dit_app, sub_can_app, sub_dit_sub),
	CONSTRAINT sub_ditte_app_fkey FOREIGN KEY (sub_dit_app) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT aub_cantieri_app_fkey FOREIGN KEY (sub_dit_app, sub_can_app) REFERENCES cantieri (can_dit, can_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT sub_ditte_sub_fkey FOREIGN KEY (sub_dit_sub) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT sub_cantieri_sub_fkey FOREIGN KEY (sub_dit_sub, sub_can_sub) REFERENCES cantieri (can_dit, can_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS sub_ditte_app ON subappalti (sub_dit_app);
CREATE INDEX IF NOT EXISTS sub_ditte_sub ON subappalti (sub_dit_sub);
CREATE INDEX IF NOT EXISTS sub_cantieri_app ON subappalti (sub_dit_app, sub_can_app);
CREATE INDEX IF NOT EXISTS sub_cantieri_sub ON subappalti (sub_dit_sub, sub_can_sub);

CREATE OR REPLACE FUNCTION sub_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.sub_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.sub_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'sub_trigger') THEN
			CREATE TRIGGER sub_trigger
			BEFORE INSERT OR UPDATE ON subappalti
			FOR EACH ROW
			EXECUTE PROCEDURE sub_trigger_function();
		END IF;
	END
$$;



/*
 *	Tabella Comuni
 */
CREATE TABLE IF NOT EXISTS comuni
(
	com_codice 			INTEGER NOT NULL DEFAULT 1 CHECK (com_codice > 0),
	com_regione 		SMALLINT NOT NULL DEFAULT 0,
	com_cod_cat 		VARCHAR (4) NOT NULL DEFAULT '',
	com_desc 			VARCHAR (50) NOT NULL DEFAULT '',
	com_cap 			VARCHAR (5) NOT NULL DEFAULT '',
	com_prov 			VARCHAR (2) NOT NULL DEFAULT '',
	com_rifiuti 		VARCHAR (7) NOT NULL DEFAULT '',
	com_uff_iva 		VARCHAR (3) NOT NULL DEFAULT '',
	com_uff_imposte		VARCHAR (3) NOT NULL DEFAULT '',
	com_uff_registro	VARCHAR (3) NOT NULL DEFAULT '',
	com_uff_registr1 	VARCHAR (3) NOT NULL DEFAULT '',
	com_cod_istat 		VARCHAR (6) NOT NULL DEFAULT '',
	com_cod_usl 		VARCHAR (3) NOT NULL DEFAULT '',
	com_cod_schps 		VARCHAR (9) NOT NULL DEFAULT '',
	com_user 			VARCHAR (20) NOT NULL DEFAULT '',
	com_last_update 	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	CONSTRAINT com_codice PRIMARY KEY (com_codice)
);
CREATE INDEX IF NOT EXISTS com_cap ON comuni (com_cap, com_codice);
CREATE INDEX IF NOT EXISTS com_codcat ON comuni (com_cod_cat, com_codice);
CREATE INDEX IF NOT EXISTS com_desc ON comuni (com_desc, com_codice);
CREATE INDEX IF NOT EXISTS com_lastupdate ON comuni (com_last_update);

CREATE OR REPLACE FUNCTION comuni_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.com_cap				= TRIM(NEW.com_cap);
		NEW.com_prov			= TRIM(NEW.com_prov);
		NEW.com_rifiuti			= TRIM(NEW.com_rifiuti);
		NEW.com_uff_iva			= TRIM(NEW.com_uff_iva);
		NEW.com_uff_imposte		= TRIM(NEW.com_uff_imposte);
		NEW.com_uff_registro	= TRIM(NEW.com_uff_registro);
		NEW.com_uff_registr1	= TRIM(NEW.com_uff_registr1);
		NEW.com_cod_istat		= TRIM(NEW.com_cod_istat);
		NEW.com_desc			= TRIM(NEW.com_desc);
		NEW.com_cod_usl			= TRIM(NEW.com_cod_usl);
		NEW.com_cod_schps		= TRIM(NEW.com_cod_schps);
		NEW.com_last_update		= NOW();
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'set_com_trigger') THEN
			CREATE TRIGGER set_com_trigger
			BEFORE INSERT OR UPDATE ON comuni
			FOR EACH ROW
			EXECUTE PROCEDURE comuni_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella norme
*/
CREATE TABLE IF NOT EXISTS norme
(
    nor_dit				INTEGER NOT NULL,
	nor_codice 		    INTEGER NOT NULL DEFAULT 1,
	nor_desc			VARCHAR (512) NOT NULL DEFAULT '',
	nor_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	nor_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT nor_codice PRIMARY KEY (nor_dit, nor_codice),
	CONSTRAINT nor_ditte_fkey FOREIGN KEY (nor_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX nor_desc ON norme (nor_desc, nor_dit, nor_codice);

CREATE OR REPLACE FUNCTION nor_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.nor_desc		= TRIM(NEW.nor_desc);
		NEW.nor_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.nor_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'nor_trigger') THEN
			CREATE TRIGGER nor_trigger
			BEFORE INSERT OR UPDATE ON norme
			FOR EACH ROW
			EXECUTE PROCEDURE nor_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella cantieri
*/
CREATE TABLE IF NOT EXISTS cantieri
(
    can_dit				INTEGER NOT NULL,
	can_codice 		    INTEGER NOT NULL DEFAULT 1,
	can_desc			VARCHAR (512) NOT NULL DEFAULT '',
	can_citta			VARCHAR (50) NOT NULL DEFAULT '',
	can_indirizzo		VARCHAR (100) NOT NULL DEFAULT '',
	can_cap				VARCHAR (5) NOT NULL DEFAULT '',
	can_prov			VARCHAR (2) NOT NULL DEFAULT '',
	can_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	can_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT can_codice PRIMARY KEY (can_dit, can_codice),
	CONSTRAINT can_ditte_fkey FOREIGN KEY (can_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX can_desc ON cantieri (can_desc, can_dit, can_codice);

CREATE OR REPLACE FUNCTION can_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.can_desc		= TRIM(NEW.can_desc);
	    NEW.can_citta		= TRIM(NEW.can_citta);
	    NEW.can_indirizzo	= TRIM(NEW.can_indirizzo);
	    NEW.can_cap			= TRIM(NEW.can_cap);
	    NEW.can_prov		= TRIM(NEW.can_prov);
		NEW.can_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.can_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'can_trigger') THEN
			CREATE TRIGGER can_trigger
			BEFORE INSERT OR UPDATE ON cantieri
			FOR EACH ROW
			EXECUTE PROCEDURE can_trigger_function();
		END IF;
	END
$$;

/*
*	Tabella Documenti
	all_type = 0
*/
CREATE TABLE IF NOT EXISTS documenti
(
    doc_dit					INTEGER NOT NULL,
	doc_codice				INTEGER NOT NULL DEFAULT 1,
	doc_dip					INTEGER NOT NULL,
	doc_livello				SMALLINT NOT NULL DEFAULT 0,
	doc_settore				SMALLINT NOT NULL DEFAULT 0,
	doc_ore					SMALLINT NOT NULL DEFAULT 0,
	doc_data				DATE,
	doc_desc				VARCHAR(512) NOT NULL DEFAULT '',
	doc_url					TEXT NOT NULL DEFAULT '',
	doc_data_rilascio		DATE,
	doc_data_scadenza		DATE,
	doc_scad_alert_before	SMALLINT NOT NULL DEFAULT 0,
	doc_scad_alert_after	SMALLINT NOT NULL DEFAULT 0,
	doc_rif_normativo		VARCHAR(512) NOT NULL DEFAULT '',
	doc_certificatore		VARCHAR(512) NOT NULL DEFAULT '',
	doc_chk					INTEGER NOT NULL DEFAULT 0,
	doc_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	doc_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT doc_codice PRIMARY KEY (doc_dit, doc_codice),
	CONSTRAINT doc_ditte_fkey FOREIGN KEY (doc_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT doc_dipendente_fkey FOREIGN KEY (doc_dit, doc_dip) REFERENCES dipendenti (dip_dit, dip_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX doc_desc ON documenti (doc_desc, doc_dit, doc_codice);
CREATE INDEX doc_dipendenti ON documenti (doc_dit, doc_dip);

CREATE OR REPLACE FUNCTION doc_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.doc_desc			= TRIM(NEW.doc_desc);
		NEW.doc_rif_normativo	= TRIM(NEW.doc_rif_normativo);
		NEW.doc_certificatore	= TRIM(NEW.doc_certificatore);
		NEW.doc_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.doc_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'doc_trigger') THEN
			CREATE TRIGGER doc_trigger
			BEFORE INSERT OR UPDATE ON documenti
			FOR EACH ROW
			EXECUTE PROCEDURE doc_trigger_function();
		END IF;
	END
$$;

/*
	Tabella Documenti Cantieri
	all_type = 1
*/
CREATE TABLE IF NOT EXISTS doccantieri
(
    dca_dit					INTEGER NOT NULL,
	dca_codice				INTEGER NOT NULL DEFAULT 1,
	dca_can					INTEGER NOT NULL,
	dca_livello				SMALLINT NOT NULL DEFAULT 0,
	dca_settore				SMALLINT NOT NULL DEFAULT 0,
	dca_data				DATE,
	dca_desc				VARCHAR(512) NOT NULL DEFAULT '',
	dca_url					TEXT NOT NULL DEFAULT '',
	dca_data_rilascio		DATE,
	dca_data_scadenza		DATE,
	dca_scad_alert_before	SMALLINT NOT NULL DEFAULT 0,
	dca_scad_alert_after	SMALLINT NOT NULL DEFAULT 0,
	dca_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	dca_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT dca_codice PRIMARY KEY (dca_dit, dca_codice),
	CONSTRAINT dca_ditte_fkey FOREIGN KEY (dca_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT dca_cantieri_fkey FOREIGN KEY (dca_dit, dca_can) REFERENCES cantieri (can_dit, can_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX dca_desc ON doccantieri (dca_desc, dca_dit, dca_codice);
CREATE INDEX dca_cantieri ON doccantieri (dca_dit, dca_can);
CREATE INDEX dca_settore ON doccantieri (dca_dit, dca_can, dca_settore, dca_codice);

CREATE OR REPLACE FUNCTION dca_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.dca_desc		= TRIM(NEW.dca_desc);
		NEW.dca_url		    = TRIM(NEW.dca_url);
		NEW.dca_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.dca_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'dca_trigger') THEN
			CREATE TRIGGER dca_trigger
			BEFORE INSERT OR UPDATE ON doccantieri
			FOR EACH ROW
			EXECUTE PROCEDURE dca_trigger_function();
		END IF;
	END
$$;

/*
	Tabella Documenti Ditta
	all_type = 2
*/
CREATE TABLE IF NOT EXISTS docditte
(
    dod_dit					INTEGER NOT NULL,
	dod_codice				INTEGER NOT NULL DEFAULT 1,
	dod_livello				SMALLINT NOT NULL DEFAULT 0,
	dod_settore				SMALLINT NOT NULL DEFAULT 0,
	dod_data				DATE,
	dod_desc				VARCHAR(512) NOT NULL DEFAULT '',
	dod_url					TEXT NOT NULL DEFAULT '',
	dod_data_rilascio		DATE,
	dod_data_scadenza		DATE,
	dod_scad_alert_before	SMALLINT NOT NULL DEFAULT 0,
	dod_scad_alert_after	SMALLINT NOT NULL DEFAULT 0,
	dod_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	dod_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT dod_codice PRIMARY KEY (dod_dit, dod_codice),
	CONSTRAINT dod_ditte_fkey FOREIGN KEY (dod_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS dod_desc ON docditte (dod_desc, dod_dit, dod_codice);
CREATE INDEX IF NOT EXISTS dod_settore ON docditte (dod_dit, dod_settore, dod_codice);

CREATE OR REPLACE FUNCTION dod_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.dod_desc	= UPPER(TRIM(NEW.dod_desc));
		NEW.dod_url		= TRIM(NEW.dod_url);
		NEW.dod_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.dod_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'dod_trigger') THEN
			CREATE TRIGGER dod_trigger
			BEFORE INSERT OR UPDATE ON docditte
			FOR EACH ROW
			EXECUTE PROCEDURE dod_trigger_function();
		END IF;
	END
$$;

/*
	Tabella Documenti Mezzi
	all_type = 1
*/
CREATE TABLE IF NOT EXISTS docmezzi
(
    dme_dit					INTEGER NOT NULL,
	dme_codice				INTEGER NOT NULL DEFAULT 1,
	dme_mez					INTEGER NOT NULL,
	dme_livello				SMALLINT NOT NULL,
	dme_data				DATE,
	dme_desc				VARCHAR(512) NOT NULL DEFAULT '',
	dme_url					TEXT NOT NULL DEFAULT '',
	dme_data_rilascio		DATE,
	dme_data_scadenza		DATE,
	dme_scad_alert_before	SMALLINT NOT NULL DEFAULT 0,
	dme_scad_alert_after	SMALLINT NOT NULL DEFAULT 0,
	dme_tipo_doc			SMALLINT NOT NULL DEFAULT 0,
	dme_protetto			SMALLINT NOT NULL DEFAULT 0,
	dme_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	dme_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	dme_user				INTEGER NOT NULL DEFAULT 0,
	CONSTRAINT dme_codice PRIMARY KEY (dme_dit, dme_codice),
	CONSTRAINT dme_ditte_fkey FOREIGN KEY (dme_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT dme_mezzi_fkey FOREIGN KEY (dme_dit, dme_mez) REFERENCES mezzi (mez_dit, mez_codice) ON UPDATE CASCADE ON DELETE CASCADE
);

ALTER TABLE docmezzi
	ADD COLUMN IF NOT EXISTS dme_protetto 	SMALLINT NOT NULL DEFAULT 0;


CREATE INDEX dme_desc ON docmezzi (dme_desc, dme_dit, dme_codice);
CREATE INDEX dme_mezzi ON docmezzi (dme_dit, dme_mez, dme_livello);
CREATE INDEX dme_type ON docmezzi (dme_dit, dme_livello, dme_mez);

CREATE OR REPLACE FUNCTION dme_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.dme_desc		= TRIM(NEW.dme_desc);
		NEW.dme_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.dme_created_at	= NOW();
    	END IF;
		IF (NEW.dme_livello <> 0) THEN
			NEW.dme_protetto = 0;
		END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'dme_trigger') THEN
			CREATE TRIGGER dme_trigger
			BEFORE INSERT OR UPDATE ON docmezzi
			FOR EACH ROW
			EXECUTE PROCEDURE dme_trigger_function();
		END IF;
	END
$$;

/*
	Tabella Scadenze Mezzi
*/
CREATE TABLE IF NOT EXISTS scamezzi
(
    scm_dit					INTEGER NOT NULL,
	scm_codice				INTEGER NOT NULL DEFAULT 1,
	scm_mez					INTEGER NOT NULL,
	scm_data				DATE,
	scm_desc				VARCHAR(512) NOT NULL DEFAULT '',
	scm_scad_alert_before	SMALLINT NOT NULL DEFAULT 0,
	scm_scad_alert_after	SMALLINT NOT NULL DEFAULT 0,
	scm_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	scm_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT scm_codice PRIMARY KEY (scm_dit, scm_codice),
	CONSTRAINT scm_ditte_fkey FOREIGN KEY (scm_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT scm_mezzi_fkey FOREIGN KEY (scm_dit, scm_mez) REFERENCES mezzi (mez_dit, mez_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX scm_desc ON scamezzi (scm_desc, scm_dit, scm_codice);
CREATE INDEX scm_mezzi ON scamezzi (scm_dit, scm_mez, scm_data, scm_codice);

CREATE OR REPLACE FUNCTION scm_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.scm_desc		= TRIM(NEW.scm_desc);
		NEW.scm_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.scm_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'scm_trigger') THEN
			CREATE TRIGGER scm_trigger
			BEFORE INSERT OR UPDATE ON scamezzi
			FOR EACH ROW
			EXECUTE PROCEDURE scm_trigger_function();
		END IF;
	END
$$;


/*
	Tabella Manutenzioni
	all_type = 4
*/
CREATE TABLE IF NOT EXISTS manutenzioni
(
    mnt_dit					INTEGER NOT NULL,
	mnt_codice				INTEGER NOT NULL DEFAULT 1,
	mnt_mez					INTEGER NOT NULL,
	mnt_tipo				SMALLINT NOT NULL,
	mnt_data				DATE,
	mnt_desc				VARCHAR(512) NOT NULL DEFAULT '',
	mnt_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	mnt_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT mnt_codice PRIMARY KEY (mnt_dit, mnt_codice),
	CONSTRAINT mnt_ditte_fkey FOREIGN KEY (mnt_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT mnt_mezzi_fkey FOREIGN KEY (mnt_dit, mnt_mez) REFERENCES mezzi (mez_dit, mez_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX mnt_desc ON manutenzioni (mnt_desc, mnt_dit, mnt_codice);
CREATE INDEX mnt_mezzi ON manutenzioni (mnt_dit, mnt_mez, mnt_data, mnt_codice);

CREATE OR REPLACE FUNCTION mnt_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.mnt_desc		= TRIM(NEW.mnt_desc);
		NEW.mnt_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.mnt_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'mnt_trigger') THEN
			CREATE TRIGGER mnt_trigger
			BEFORE INSERT OR UPDATE ON manutenzioni
			FOR EACH ROW
			EXECUTE PROCEDURE mnt_trigger_function();
		END IF;
	END
$$;

/*
	Tabella Documenti Modelli
	all_type = 5
*/
CREATE TABLE IF NOT EXISTS docmodelli
(
    dmo_dit					INTEGER NOT NULL,
	dmo_codice				INTEGER NOT NULL DEFAULT 1,
	dmo_mod					INTEGER NOT NULL,
	dmo_livello				SMALLINT NOT NULL,
	dmo_data				DATE,
	dmo_desc				VARCHAR(512) NOT NULL DEFAULT '',
	dmo_url					TEXT NOT NULL DEFAULT '',
	dmo_data_rilascio		DATE,
	dmo_data_scadenza		DATE,
	dmo_scad_alert_before	SMALLINT NOT NULL DEFAULT 0,
	dmo_scad_alert_after	SMALLINT NOT NULL DEFAULT 0,
	dmo_serial_start		VARCHAR(25) NOT NULL  DEFAULT '',
	dmo_serial_stop			VARCHAR(25) NOT NULL  DEFAULT '',
	dmo_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	dmo_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	dmo_user 				INTEGER NOT NULL DEFAULT 0,
	CONSTRAINT dmo_codice PRIMARY KEY (dmo_dit, dmo_codice),
	CONSTRAINT dmo_ditte_fkey FOREIGN KEY (dmo_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT dmo_modelli_fkey FOREIGN KEY (dmo_dit, dmo_mod) REFERENCES modelli (mod_dit, mod_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX dmo_desc ON docmodelli (dmo_desc, dmo_dit, dmo_codice);
CREATE INDEX dmo_modelli ON docmodelli (dmo_dit, dmo_mod, dmo_livello);
CREATE INDEX dmo_type ON docmodelli (dmo_dit, dmo_livello, dmo_mod);

ALTER TABLE docmodelli
	DROP COLUMN IF EXISTS dmo_serial_start,
    DROP COLUMN IF EXISTS dmo_serial_stop,
    ADD COLUMN IF NOT EXISTS dmo_user INTEGER NOT NULL DEFAULT 0;


CREATE OR REPLACE FUNCTION dmo_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.dmo_desc		= TRIM(NEW.dmo_desc);
		NEW.dmo_url			= TRIM(NEW.dmo_url);
		NEW.dmo_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.dmo_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'dmo_trigger') THEN
			CREATE TRIGGER dmo_trigger
			BEFORE INSERT OR UPDATE ON docmodelli
			FOR EACH ROW
			EXECUTE PROCEDURE dmo_trigger_function();
		END IF;
	END
$$;


/*
 	Tabella di correlazione tra documenti dei modelli e i numeri seriali
 */
CREATE TABLE IF NOT EXISTS modserial
(
    mse_dit					INTEGER NOT NULL,
    mse_dmo					INTEGER NOT NULL,
	mse_codice				INTEGER NOT NULL DEFAULT 1,
	mse_cod_for				VARCHAR (25) NOT NULL  DEFAULT 0,
	mse_serial_start		VARCHAR(25) NOT NULL  DEFAULT '',
	mse_serial_stop			VARCHAR(25) NOT NULL  DEFAULT '',
	mse_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	mse_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	mse_user				INTEGER NOT NULL DEFAULT 0,
	CONSTRAINT mse_codice PRIMARY KEY (mse_dit, mse_dmo, mse_codice),
	CONSTRAINT mse_ditte_fkey FOREIGN KEY (mse_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT mse_docmo_fkey FOREIGN KEY (mse_dit, mse_dmo) REFERENCES docmodelli (dmo_dit, dmo_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX mse_ditte ON modserial (mse_dit);
CREATE INDEX mse_docmodelli ON modserial (mse_dit, mse_dmo, mse_codice);
CREATE INDEX mse_serial ON modserial (mse_dit, mse_dmo, mse_cod_for, mse_serial_start);

CREATE OR REPLACE FUNCTION mse_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.mse_cod_for = UPPER(TRIM(NEW.mse_cod_for));
	    NEW.mse_serial_start = UPPER(TRIM(NEW.mse_serial_start));
	    NEW.mse_serial_stop = UPPER(TRIM(NEW.mse_serial_stop));
		NEW.mse_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.mse_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'mse_trigger') THEN
			CREATE TRIGGER mse_trigger
			BEFORE INSERT OR UPDATE ON modserial
			FOR EACH ROW
			EXECUTE PROCEDURE mse_trigger_function();
		END IF;
	END
$$;

/*
	Tabella Video Modelli
*/
CREATE TABLE IF NOT EXISTS videomodelli
(
    vmo_dit					INTEGER NOT NULL,
	vmo_codice				INTEGER NOT NULL DEFAULT 1,
	vmo_mod					INTEGER NOT NULL,
	vmo_livello				SMALLINT NOT NULL,
	vmo_data				DATE,
	vmo_desc				VARCHAR(512) NOT NULL DEFAULT '',
	vmo_url					TEXT NOT NULL DEFAULT '',
	vmo_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	vmo_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT vmo_codice PRIMARY KEY (vmo_dit, vmo_codice),
	CONSTRAINT vmo_ditte_fkey FOREIGN KEY (vmo_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT vmo_modelli_fkey FOREIGN KEY (vmo_dit, vmo_mod) REFERENCES modelli (mod_dit, mod_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX vmo_desc ON videomodelli (vmo_desc, vmo_dit, vmo_codice);
CREATE INDEX vmo_modelli ON videomodelli (vmo_dit, vmo_mod, vmo_livello);
CREATE INDEX vmo_type ON videomodelli (vmo_dit, vmo_livello, vmo_mod);

CREATE OR REPLACE FUNCTION vmo_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.vmo_desc		= TRIM(NEW.vmo_desc);
		NEW.vmo_url			= TRIM(NEW.vmo_url);
		NEW.vmo_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.vmo_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'vmo_trigger') THEN
			CREATE TRIGGER vmo_trigger
			BEFORE INSERT OR UPDATE ON videomodelli
			FOR EACH ROW
			EXECUTE PROCEDURE vmo_trigger_function();
		END IF;
	END
$$;



/*
	Tabella Immagini Modelli
*/
CREATE TABLE IF NOT EXISTS imgmodelli
(
	img_dit			INTEGER NOT NULL,
	img_codice		INTEGER NOT NULL,
	img_formato		SMALLINT NOT NULL DEFAULT 0,
	img_tipo		SMALLINT NOT NULL DEFAULT 0,
	img_bytes_size	INTEGER NOT NULL DEFAULT 0,
	img_created_at	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_last_update	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_data		BYTEA,
	CONSTRAINT img_mod_codice PRIMARY KEY (img_dit, img_codice, img_formato),
	CONSTRAINT img_mod_ditte_fkey FOREIGN KEY (img_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT imm_modelli_fkey FOREIGN KEY (img_dit, img_codice) REFERENCES modelli (mod_dit, mod_codice) ON UPDATE CASCADE ON DELETE CASCADE
)  PARTITION BY LIST (img_dit);

CREATE INDEX IF NOT EXISTS img_mod_ditte ON imgmodelli (img_dit);
CREATE INDEX IF NOT EXISTS imm_modelli ON imgmodelli (img_dit, img_codice);

CREATE OR REPLACE FUNCTION img_mod_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.img_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.img_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'img_mod_trigger') THEN
			CREATE TRIGGER img_mod_trigger
			BEFORE INSERT OR UPDATE ON imgmodelli
			FOR EACH ROW
			EXECUTE PROCEDURE img_mod_trigger_function();
		END IF;
	END
$$;


/*
	Tabella Immagini Mezzi
*/
CREATE TABLE IF NOT EXISTS imgmezzi
(
	img_dit			INTEGER NOT NULL,
	img_codice		INTEGER NOT NULL,
	img_formato		SMALLINT NOT NULL DEFAULT 0,
	img_tipo		SMALLINT NOT NULL DEFAULT 0,
	img_bytes_size	INTEGER NOT NULL DEFAULT 0,
	img_created_at	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_last_update	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_data		BYTEA,
	CONSTRAINT img_codice PRIMARY KEY (img_dit, img_codice, img_formato),
	CONSTRAINT img_ditte_fkey FOREIGN KEY (img_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT img_mezzi_fkey FOREIGN KEY (img_dit, img_codice) REFERENCES mezzi (mez_dit, mez_codice) ON UPDATE CASCADE ON DELETE CASCADE
)  PARTITION BY LIST (img_dit);

CREATE INDEX img_ditte ON imgmezzi (img_dit);
CREATE INDEX img_mezzi ON imgmezzi (img_dit, img_codice);

CREATE OR REPLACE FUNCTION img_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.img_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.img_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'img_trigger') THEN
			CREATE TRIGGER img_trigger
			BEFORE INSERT OR UPDATE ON imgmezzi
			FOR EACH ROW
			EXECUTE PROCEDURE img_trigger_function();
		END IF;
	END
$$;


/*
	Tabella Immagini Utenti
*/
CREATE TABLE IF NOT EXISTS imgutenti
(
	img_dit			INTEGER NOT NULL DEFAULT 0,
	img_codice		INTEGER NOT NULL CHECK(img_codice > 0),
	img_formato		SMALLINT NOT NULL DEFAULT 0,
	img_tipo		SMALLINT NOT NULL DEFAULT 0,
	img_bytes_size	INTEGER NOT NULL DEFAULT 0,
	img_created_at	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_last_update	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_data		BYTEA,
	CONSTRAINT imu_codice PRIMARY KEY (img_dit, img_codice, img_formato)
);

CREATE OR REPLACE FUNCTION imu_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.img_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.img_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'imu_trigger') THEN
			CREATE TRIGGER imu_trigger
			BEFORE INSERT OR UPDATE ON imgutenti
			FOR EACH ROW
			EXECUTE PROCEDURE imu_trigger_function();
		END IF;
	END
$$;



/*
*	Creazione Tablla artimag
*/
CREATE TABLE IF NOT EXISTS artimag
(
	img_codice				VARCHAR (15) NOT NULL DEFAULT '',
	img_tipo				SMALLINT NOT NULL DEFAULT 0,
	img_formato				SMALLINT NOT NULL DEFAULT 0,
	img_file_data			TIMESTAMP,
	img_bytes_size			INTEGER NOT NULL DEFAULT 0,
	img_created_at			TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_last_update			TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_data				BYTEA,
	CONSTRAINT img_formato_codice PRIMARY KEY (img_formato, img_codice)
);
CREATE INDEX img_articolo ON artimag (img_codice);

CREATE OR REPLACE FUNCTION artimag_trigger_function()
	RETURNS TRIGGER AS $$
	BEGIN
		NEW.img_codice	= UPPER(TRIM(NEW.img_codice));
		NEW.img_last_update	= NOW();
	RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'artimg_trigger') THEN
			CREATE TRIGGER img_trigger
			BEFORE INSERT OR UPDATE ON artimag
			FOR EACH ROW
				EXECUTE PROCEDURE artimag_trigger_function();
		END IF;
	END
$$;


CREATE TABLE IF NOT EXISTS docmodelli
(
    dmo_dit					INTEGER NOT NULL,
	dmo_codice				INTEGER NOT NULL DEFAULT 1,
	dmo_mod					INTEGER NOT NULL,
	dmo_livello				SMALLINT NOT NULL,
	dmo_data				DATE,
	dmo_desc				VARCHAR(512) NOT NULL DEFAULT '',
	dmo_url					TEXT NOT NULL DEFAULT '',
	dmo_data_rilascio		DATE,
	dmo_data_scadenza		DATE,
	dmo_scad_alert_before	SMALLINT NOT NULL DEFAULT 0,
	dmo_scad_alert_after	SMALLINT NOT NULL DEFAULT 0,
	dmo_serial_start		VARCHAR(25) NOT NULL  DEFAULT '',
	dmo_serial_stop			VARCHAR(25) NOT NULL  DEFAULT '',
	dmo_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	dmo_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT dmo_codice PRIMARY KEY (dmo_dit, dmo_codice),
	CONSTRAINT dmo_ditte_fkey FOREIGN KEY (dmo_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT dmo_modelli_fkey FOREIGN KEY (dmo_dit, dmo_mod) REFERENCES modelli (mod_dit, mod_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX dmo_desc ON docmodelli (dmo_desc, dmo_dit, dmo_codice);
CREATE INDEX dmo_modelli ON docmodelli (dmo_dit, dmo_mod, dmo_livello);
CREATE INDEX dmo_type ON docmodelli (dmo_dit, dmo_livello, dmo_mod);

CREATE OR REPLACE FUNCTION dmo_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.dmo_desc		= TRIM(NEW.dmo_desc);
		NEW.dmo_url			= TRIM(NEW.dmo_url);
		NEW.dmo_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.dmo_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'dmo_trigger') THEN
			CREATE TRIGGER dmo_trigger
			BEFORE INSERT OR UPDATE ON docmodelli
			FOR EACH ROW
			EXECUTE PROCEDURE dmo_trigger_function();
		END IF;
	END
$$;

/*
 	Giornale dei Lavori
 */
CREATE TABLE IF NOT EXISTS giornalelav
(
    gio_dit					INTEGER NOT NULL,
    gio_codice				INTEGER NOT NULL DEFAULT 1,
    gio_can					INTEGER NOT NULL,
    gio_data				DATE NOT NULL,
    gio_desc				TEXT NOT NULL DEFAULT '',
    gio_osservazioni		TEXT NOT NULL DEFAULT '',
    gio_meteo				SMALLINT NOT NULL DEFAULT 0,
    gio_temperatura_min		REAL NOT NULL DEFAULT 0,
    gio_temperatura_max		REAL NOT NULL DEFAULT 0,
	gio_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	gio_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	gio_utente				INTEGER NOT NULL DEFAULT 0,
	CONSTRAINT gio_codice PRIMARY KEY (gio_dit, gio_codice),
	CONSTRAINT gio_ditte_fkey FOREIGN KEY (gio_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT gio_cantieri_fkey FOREIGN KEY (gio_dit, gio_can) REFERENCES cantieri (can_dit, can_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX gio_data ON giornalelav (gio_dit, gio_can, gio_data, gio_codice);

CREATE OR REPLACE FUNCTION gio_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.gio_desc		 = TRIM(NEW.gio_desc);
		NEW.gio_osservazioni = TRIM(NEW.gio_osservazioni);
		NEW.gio_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.gio_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';
_dit = ?
DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'gio_trigger') THEN
			CREATE TRIGGER gio_trigger
			BEFORE INSERT OR UPDATE ON giornalelav
			FOR EACH ROW
			EXECUTE PROCEDURE gio_trigger_function();
		END IF;
	END
$$;

/*
 	Mezzi Utilizzati su giornale lavori
 */
CREATE TABLE IF NOT EXISTS giornalemez
(
    gme_codice				BIGSERIAL,
    gme_gio					INTEGER NOT NULL,
    gme_dit					INTEGER NOT NULL,
    gme_mez					INTEGER NOT NULL,
    gme_mez_dit				INTEGER NOT NULL,
	gme_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	gme_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	gme_utente				INTEGER NOT NULL DEFAULT 0,
	CONSTRAINT gme_codice PRIMARY KEY (gme_codice),
	CONSTRAINT gme_giornale_fkey FOREIGN KEY (gme_dit, gme_gio) REFERENCES giornalelav (gio_dit, gio_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT gme_ditte_fkey FOREIGN KEY (gme_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT gme_ditte_mez_fkey FOREIGN KEY (gme_mez_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT gme_mezzi_fkey FOREIGN KEY (gme_mez_dit, gme_mez) REFERENCES mezzi (mez_dit, mez_codice) ON UPDATE CASCADE ON DELETE RESTRICT,
	CONSTRAINT gme_mezzi_check UNIQUE (gme_gio, gme_mez_dit, gme_mez)
);

CREATE INDEX IF NOT EXISTS gme_giornale ON giornalemez (gme_dit, gme_gio, gme_codice);
CREATE INDEX IF NOT EXISTS gme_ditte ON giornalemez (gme_dit, gme_codice);
CREATE INDEX IF NOT EXISTS gme_ditte_mezzi ON giornalemez (gme_mez_dit, gme_codice);
CREATE INDEX IF NOT EXISTS gme_mezzi ON giornalemez (gme_mez_dit, gme_mez, gme_codice);

CREATE OR REPLACE FUNCTION gme_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.gme_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.gme_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'gme_trigger') THEN
			CREATE TRIGGER gme_trigger
			BEFORE INSERT OR UPDATE ON giornalemez
			FOR EACH ROW
			EXECUTE PROCEDURE gme_trigger_function();
		END IF;
	END
$$;

/*
 	Dipendenti Utilizzati su giornale lavori
 */
CREATE TABLE IF NOT EXISTS giornaledip
(
    gdi_codice				BIGSERIAL,
    gdi_gio					INTEGER NOT NULL,
    gdi_dit					INTEGER NOT NULL,
    gdi_dip					INTEGER NOT NULL,
    gdi_dip_dit				INTEGER NOT NULL,
	gdi_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	gdi_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	gdi_utente				INTEGER NOT NULL DEFAULT  0,
	CONSTRAINT gdi_codice PRIMARY KEY (gdi_codice),
	CONSTRAINT gdi_giornale_fkey FOREIGN KEY (gdi_dit, gdi_gio) REFERENCES giornalelav (gio_dit, gio_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT gdi_ditte_fkey FOREIGN KEY (gdi_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT gdi_ditte_dip_fkey FOREIGN KEY (gdi_dip_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT gdi_dipendenti_fkey FOREIGN KEY (gdi_dip_dit, gdi_dip) REFERENCES dipendenti (dip_dit, dip_codice) ON UPDATE CASCADE ON DELETE RESTRICT,
	CONSTRAINT gdi_dipendenti_check UNIQUE (gdi_gio, gdi_dip_dit, gdi_dip)
);
CREATE INDEX IF NOT EXISTS gdi_giornale ON giornaledip (gdi_dit, gdi_gio, gdi_codice);
CREATE INDEX IF NOT EXISTS gdi_ditte ON giornaledip (gdi_dit, gdi_codice);
CREATE INDEX IF NOT EXISTS gdi_ditte_dip ON giornaledip (gdi_dip_dit, gdi_codice);
CREATE INDEX IF NOT EXISTS gdi_dipendenti ON giornaledip (gdi_dip_dit, gdi_dip, gdi_codice);

CREATE OR REPLACE FUNCTION gdi_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.gdi_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.gdi_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'gdi_trigger') THEN
			CREATE TRIGGER gdi_trigger
			BEFORE INSERT OR UPDATE ON giornaledip
			FOR EACH ROW
			EXECUTE PROCEDURE gdi_trigger_function();
		END IF;
	END
$$;

/*
	Tabella Immagini Giornale Lavori
*/
CREATE TABLE IF NOT EXISTS giornaleimg
(
	img_dit			INTEGER NOT NULL,
	img_codice		INTEGER NOT NULL,
	img_formato		SMALLINT NOT NULL DEFAULT 0,
	img_tipo		SMALLINT NOT NULL DEFAULT 0,
	img_bytes_size	INTEGER NOT NULL DEFAULT 0,
	img_created_at	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_last_update	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	img_utente		INTEGER NOT NULL DEFAULT  0,
	img_data		BYTEA,
	CONSTRAINT img_gio_codice PRIMARY KEY (img_dit, img_codice, img_formato),
	CONSTRAINT img_gio_ditte_fkey FOREIGN KEY (img_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT img_giornale_fkey FOREIGN KEY (img_dit, img_codice) REFERENCES giornalelav (gio_dit, gio_codice) ON UPDATE CASCADE ON DELETE CASCADE
)  PARTITION BY LIST (img_dit);

CREATE INDEX IF NOT EXISTS img_gio_ditte ON giornaleimg (img_dit);
CREATE INDEX IF NOT EXISTS img_giornale ON giornaleimg (img_dit, img_codice);

CREATE OR REPLACE FUNCTION img_gio_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.img_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.img_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'img_gio_trigger') THEN
			CREATE TRIGGER img_gio_trigger
			BEFORE INSERT OR UPDATE ON giornaleimg
			FOR EACH ROW
			EXECUTE PROCEDURE img_gio_trigger_function();
		END IF;
	END
$$;

/*
 	Certificati di Pagamento
 */
CREATE TABLE IF NOT EXISTS certificatipag
(
    cpa_dit					INTEGER NOT NULL,
    cpa_codice				INTEGER NOT NULL DEFAULT 1,
    cpa_can					INTEGER NOT NULL,
    cpa_data				DATE NOT NULL,
    cpa_desc				TEXT NOT NULL DEFAULT '',
    cpa_sub				    INTEGER NOT NULL,
    cpa_mese				SMALLINT NOT NULL DEFAULT 0,
    cpa_importo				DOUBLE PRECISION NOT NULL DEFAULT 0,
    cpa_firma_sub			SMALLINT NOT NULL DEFAULT 0,
    cpa_firma_dir			SMALLINT NOT NULL DEFAULT 0,
    cpa_firma_amm			SMALLINT NOT NULL DEFAULT 0,
    cpa_num_fat				VARCHAR (20) NOT NULL  DEFAULT '',
    cpa_data_fat			DATE,
	cpa_created_at			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	cpa_last_update			TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	cpa_utente				INTEGER NOT NULL DEFAULT 0,
	CONSTRAINT cpa_codice PRIMARY KEY (cpa_dit, cpa_codice),
	CONSTRAINT cpa_ditte_fkey FOREIGN KEY (cpa_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT cpa_subappaltatori_fkey FOREIGN KEY (cpa_sub) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT cpa_cantieri_fkey FOREIGN KEY (cpa_dit, cpa_can) REFERENCES cantieri (can_dit, can_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX cpa_ditte ON certificatipag (cpa_dit, cpa_codice);
CREATE INDEX cpa_subbappaltatori ON certificatipag (cpa_sub, cpa_dit, cpa_codice);
CREATE INDEX cpa_cantieri ON certificatipag (cpa_dit, cpa_can, cpa_codice);
CREATE INDEX cpa_data ON certificatipag (cpa_dit, cpa_can, cpa_data, cpa_codice);
CREATE INDEX cpa_sub_data ON certificatipag (cpa_dit, cpa_can, cpa_sub, cpa_data, cpa_codice);

CREATE OR REPLACE FUNCTION cpa_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.cpa_desc		= TRIM(NEW.cpa_desc);
		NEW.cpa_num_fat 	= TRIM(NEW.cpa_num_fat);
		NEW.cpa_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.cpa_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'cpa_trigger') THEN
			CREATE TRIGGER cpa_trigger
			BEFORE INSERT OR UPDATE ON certificatipag
			FOR EACH ROW
			EXECUTE PROCEDURE cpa_trigger_function();
		END IF;
	END
$$;

/*
*	Creazione Tabella checlist mansioni
*/
CREATE TABLE IF NOT EXISTS chkmansioni
(
	mac_man 		    INTEGER NOT NULL,
	mac_chk				INTEGER NOT NULL,
	mac_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	mac_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	mac_user			INTEGER NOT NULL DEFAULT 0,
	CONSTRAINT mac_codice PRIMARY KEY (mac_man, mac_chk),
	CONSTRAINT mac_mansioni_fkey FOREIGN KEY (mac_man) REFERENCES mansioni (man_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT mac_checklist_fkey FOREIGN KEY (mac_chk) REFERENCES checklist (chk_codice) ON UPDATE CASCADE ON DELETE CASCADE

);
CREATE INDEX IF NOT EXISTS mac_checklist ON chkmansioni (mac_chk);
CREATE INDEX IF NOT EXISTS mac_mansioni ON chkmansioni (mac_man);

CREATE OR REPLACE FUNCTION mac_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.mac_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.mac_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'mac_trigger') THEN
			CREATE TRIGGER mac_trigger
			BEFORE INSERT OR UPDATE ON chkmansioni
			FOR EACH ROW
			EXECUTE PROCEDURE mac_trigger_function();
		END IF;
	END
$$;


/*
*	Creazione Tabella password di download
*/
CREATE TABLE IF NOT EXISTS downloadpwd
(
    pwd_codice			BIGSERIAL PRIMARY KEY,
	pwd_dit 		    INTEGER NOT NULL,
	pwd_password		TEXT NOT NULL DEFAULT '',
	pwd_scadenza		DATE,
	pwd_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	pwd_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	pwd_utente			INTEGER NOT NULL DEFAULT 0,
	CONSTRAINT pwd_ditta_fkey FOREIGN KEY (pwd_dit) REFERENCES ditte (dit_codice) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS pwd_ditta ON downloadpwd (pwd_dit);
CREATE UNIQUE INDEX IF NOT EXISTS pwd_password ON downloadpwd (pwd_dit, pwd_password);

CREATE OR REPLACE FUNCTION pwd_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
	    NEW.pwd_password = TRIM(NEW.pwd_password);
		NEW.pwd_last_update	= NOW();
   	    IF  (TG_OP = 'INSERT') THEN
           NEW.pwd_created_at	= NOW();
    	END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'pwd_trigger') THEN
			CREATE TRIGGER pwd_trigger
			BEFORE INSERT OR UPDATE ON downloadpwd
			FOR EACH ROW
			EXECUTE PROCEDURE pwd_trigger_function();
		END IF;
	END
$$;



