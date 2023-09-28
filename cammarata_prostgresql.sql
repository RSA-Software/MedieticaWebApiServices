CREATE TABLE IF NOT EXISTS utenti (
	ute_codice			INTEGER NOT NULL DEFAULT 1,
	ute_rag_soc1		VARCHAR (50) NOT NULL DEFAULT '',
	ute_rag_soc2		VARCHAR (50) NOT NULL DEFAULT '',
	ute_desc			VARCHAR (101) NOT NULL DEFAULT '',
	ute_indirizzo		VARCHAR (100) NOT NULL DEFAULT '',
	ute_cap				VARCHAR (5) NOT NULL DEFAULT '',
	ute_prov			VARCHAR (2) NOT NULL DEFAULT '',
	ute_citta			VARCHAR (50) NOT NULL DEFAULT '',
	ute_email			VARCHAR (80) NOT NULL DEFAULT '',
	ute_tel				VARCHAR (15) NOT NULL DEFAULT '',
	ute_cel				VARCHAR (15) NOT NULL DEFAULT '',
	ute_created_at		TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	ute_last_update		TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	ute_password		VARCHAR (64) NOT NULL DEFAULT '',
	ute_pwd_decoded		SMALLINT NOT NULL DEFAULT 0,
	ute_level			INTEGER NOT NULL DEFAULT 0,
	ute_credito			DOUBLE PRECISION NOT NULL DEFAULT 0,
	CONSTRAINT ute_codice PRIMARY KEY (ute_codice)
);
CREATE INDEX IF NOT EXISTS ute_desc_idx ON utenti (ute_desc);
CREATE UNIQUE INDEX IF NOT EXISTS ute_email_idx ON utenti (ute_email, ute_password);

CREATE OR REPLACE FUNCTION ute_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.ute_rag_soc1	= UPPER(TRIM(NEW.ute_rag_soc1));
		NEW.ute_rag_soc2	= UPPER(TRIM(NEW.ute_rag_soc2));
		NEW.ute_desc		= UPPER(TRIM(CONCAT(NEW.ute_rag_soc1, ' ', NEW.ute_rag_soc2)));
		NEW.ute_cap			= UPPER(TRIM(NEW.ute_cap));
		NEW.ute_prov		= UPPER(TRIM(NEW.ute_prov));
		NEW.ute_tel			= UPPER(TRIM(NEW.ute_tel));
		NEW.ute_cel			= UPPER(TRIM(NEW.ute_cel));
		NEW.ute_email		= LOWER(TRIM(NEW.ute_email));
		NEW.ute_indirizzo	= UPPER(TRIM(NEW.ute_indirizzo));
		NEW.ute_created_at	= NOW();
		NEW.ute_last_update	= NOW();
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

INSERT INTO utenti (ute_codice, ute_rag_soc1, ute_rag_soc2, ute_email, ute_password, ute_pwd_decoded, ute_level )
VALUES (1, 'CAPIZZI', 'FILIPPO', 'capizzi@rsaweb.com', 'giordano', 1, 2);

INSERT INTO utenti (ute_codice, ute_rag_soc1, ute_rag_soc2, ute_email, ute_password, ute_pwd_decoded, ute_level )
VALUES (2, 'MARCO', 'CAMMARATA', 'marco.cammarata12@gmail.com', 'marco', 1, 2);

INSERT INTO utenti (ute_codice, ute_rag_soc1, ute_rag_soc2, ute_email, ute_password, ute_pwd_decoded, ute_level )
VALUES (3, 'MANNO', 'ANDREA', 'andream.rsaweb@gmail.com', 'andrea', 1, 2);


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
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'img_trigger') THEN
			CREATE TRIGGER img_trigger
			BEFORE INSERT OR UPDATE ON artimag
			FOR EACH ROW
				EXECUTE PROCEDURE artimag_trigger_function();
		END IF;
	END
$$;




/*
*	Creazione Tablla catmerc
*/
CREATE TABLE IF NOT EXISTS catmerc
(
	mer_codice 		INTEGER NOT NULL DEFAULT 1,
	mer_desc 		VARCHAR (50) NOT NULL DEFAULT '',
	mer_created_at	TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	mer_last_update	TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT mer_codice PRIMARY KEY (mer_codice)
);


CREATE INDEX mer_desc ON catmerc (mer_desc, mer_codice);

CREATE OR REPLACE FUNCTION mer_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.mer_desc     	= UPPER(TRIM(NEW.mer_desc));
		NEW.mer_last_update	= NOW();
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'mer_trigger') THEN
			CREATE TRIGGER mer_trigger
			BEFORE INSERT OR UPDATE ON catmerc
			FOR EACH ROW
			EXECUTE PROCEDURE mer_trigger_function();
		END IF;
	END
$$;


/*
 	Creazione tabella distributori
 */
CREATE TABLE IF NOT EXISTS distributori
(
	dis_codice 		INTEGER NOT NULL DEFAULT 1,
	dis_desc 		VARCHAR (50) NOT NULL DEFAULT '',
	dis_disabled	SMALLINT NOT NULL DEFAULT '0',
	dis_created_at	TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	dis_last_update	TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT dis_codice PRIMARY KEY (dis_codice)
);

CREATE INDEX dis_desc ON distributori (dis_desc, dis_codice);

CREATE OR REPLACE FUNCTION dis_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.dis_desc     	= UPPER(TRIM(NEW.dis_desc));
		NEW.dis_last_update	= NOW();
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'dis_trigger') THEN
			CREATE TRIGGER dis_trigger
			BEFORE INSERT OR UPDATE ON distributori
			FOR EACH ROW
			EXECUTE PROCEDURE dis_trigger_function();
		END IF;
	END
$$;

/*
 	Creazione tabella artanag
 */
CREATE TABLE IF NOT EXISTS artanag
(
	ana_codice 			VARCHAR(15) NOT NULL,
	ana_desc 			VARCHAR (100) NOT NULL DEFAULT '',
	ana_ingredienti		VARCHAR (1000) NOT NULL DEFAULT '',
	ana_sell_price		DOUBLE PRECISION NOT NULL DEFAULT 0,
	ana_purchase_price	DOUBLE PRECISION NOT NULL DEFAULT 0,
	ana_mer				INTEGER,
	ana_created_at		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	ana_last_update		TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT ana_codice PRIMARY KEY (ana_codice),
	CONSTRAINT ana_mer_fkey FOREIGN KEY (ana_mer) REFERENCES catmerc (mer_codice) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX ana_desc ON artanag (ana_desc, ana_codice);
CREATE INDEX ana_catmerc ON artanag (ana_mer, ana_codice);

CREATE OR REPLACE FUNCTION ana_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.ana_desc     	= UPPER(TRIM(NEW.ana_desc));
		NEW.ana_ingredienti = TRIM(NEW.ana_ingredienti);
		NEW.ana_last_update	= NOW();
		IF (NEW.ana_mer = 0) THEN
			NEW.ana_mer = NULL;
		END IF;
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'ana_trigger') THEN
			CREATE TRIGGER ana_trigger
			BEFORE INSERT OR UPDATE ON artanag
			FOR EACH ROW
			EXECUTE PROCEDURE ana_trigger_function();
		END IF;
	END
$$;

/*
 	Creazione tabella artcounter
 */
CREATE TABLE IF NOT EXISTS artcounter
(
	aco_ana	      	VARCHAR(15)      NOT NULL,
	aco_dis         INTEGER          NOT NULL,
	aco_esistenza	DOUBLE PRECISION NOT NULL DEFAULT 0,
	aco_created_at	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	aco_last_update	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	CONSTRAINT aco_codice_dis PRIMARY KEY (aco_ana, aco_dis),
	CONSTRAINT aco_dis_key FOREIGN KEY (aco_dis) REFERENCES distributori (dis_codice) ON UPDATE CASCADE ON DELETE CASCADE ,
	CONSTRAINT aco_ana_key FOREIGN KEY (aco_ana) REFERENCES artanag (ana_codice) ON UPDATE CASCADE ON DELETE CASCADE
);

CREATE INDEX aco_artanag ON artcounter(aco_ana);
CREATE INDEX aco_distributore ON artcounter(aco_dis);

CREATE OR REPLACE FUNCTION aco_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.aco_ana     	= UPPER(TRIM(NEW.aco_ana));
		NEW.aco_last_update	= NOW();
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'aco_trigger') THEN
			CREATE TRIGGER aco_trigger
			BEFORE INSERT OR UPDATE ON artcounter
			FOR EACH ROW
			EXECUTE PROCEDURE aco_trigger_function();
		END IF;
	END
$$;

/*
 	Creazione tabella movimenti
 */
CREATE TABLE IF NOT EXISTS movimenti
(
    mov_codice		INTEGER NOT NULL DEFAULT 1,
	mov_ana		   	VARCHAR(15)      NOT NULL,
	mov_dis         INTEGER          NOT NULL,
	mov_ute         INTEGER          NOT NULL,
	mov_qta			DOUBLE PRECISION NOT NULL DEFAULT 0,
	mov_price		DOUBLE PRECISION NOT NULL DEFAULT 0,
	mov_total		DOUBLE PRECISION NOT NULL DEFAULT 0,
	mov_created_at	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	mov_last_update	TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
	CONSTRAINT mov_codice PRIMARY KEY (mov_codice),
	CONSTRAINT mov_dis_key FOREIGN KEY (mov_dis) REFERENCES distributori (dis_codice) ON UPDATE CASCADE ON DELETE CASCADE ,
	CONSTRAINT mov_ana_key FOREIGN KEY (mov_ana) REFERENCES artanag (ana_codice) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT mov_ute_key FOREIGN KEY (mov_ute) REFERENCES utenti (ute_codice) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE OR REPLACE FUNCTION mov_trigger_function()
RETURNS TRIGGER AS $$
	BEGIN
		NEW.mov_ana   	= UPPER(TRIM(NEW.mov_ana));
		NEW.mov_last_update = NOW();
		NEW.mov_total = ROUND(CAST((@ NEW.mov_qta * NEW.mov_price) AS numeric), 2);
		UPDATE artcounter SET aco_esistenza = aco_esistenza + NEW.mov_qta WHERE aco_ana = NEW.mov_ana AND aco_dis = NEW.mov_dis; 
        IF NOT FOUND THEN 
        	INSERT INTO artcounter (aco_ana, aco_dis, aco_esistenza) VALUES (NEW.mov_ana, NEW.mov_dis, NEW.mov_qta); 
        END IF; 
		RETURN NEW;
	END;
$$ language 'plpgsql';

DO $$
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'mov_trigger') THEN
			CREATE TRIGGER mov_trigger
			BEFORE INSERT OR UPDATE ON movimenti
			FOR EACH ROW
			EXECUTE PROCEDURE mov_trigger_function();
		END IF;
	END
$$;