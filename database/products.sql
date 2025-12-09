BEGIN;

-- Utility: auto-update updated_at
CREATE OR REPLACE FUNCTION set_updated_at() RETURNS trigger AS $$
BEGIN
  NEW.updated_at := NOW();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ========================
-- 1) TABLES (NO FK YET)
-- ========================

-- users
CREATE TABLE IF NOT EXISTS users (
  id         BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  name       VARCHAR(150) NOT NULL,
  email      VARCHAR(150) NOT NULL UNIQUE,
  phone      VARCHAR(20),
  password   VARCHAR(255) NOT NULL,
  avatar     VARCHAR(150) NOT NULL,
  role       VARCHAR(20)  NOT NULL DEFAULT 'user',
  status     SMALLINT     NOT NULL DEFAULT 1, -- 1=active,0=blocked
  created_at TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
  CONSTRAINT users_role_chk   CHECK (role IN ('user','admin')),
  CONSTRAINT users_status_chk CHECK (status IN (0,1))
);
CREATE INDEX IF NOT EXISTS idx_users_phone ON users(phone);
CREATE TRIGGER trg_users_updated_at
BEFORE UPDATE ON users FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- user_wallets
CREATE TABLE IF NOT EXISTS user_wallets (
  id                   BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  user_id              BIGINT NOT NULL,
  provider             VARCHAR(150) NOT NULL, -- momo, zalopay, vnpay, stripe, paypal
  card_holder_name     VARCHAR(150),
  card_number          VARCHAR(150), -- masked only: xxxx-xxxx-xxxx-1234
  account_name         VARCHAR(150),
  expmonth             INT,
  expyear              INT,
  cvvhash              VARCHAR(255),
  external_customer_id VARCHAR(150),
  external_method_id   VARCHAR(150),
  currency             CHAR(3) NOT NULL DEFAULT 'VND',
  is_default           SMALLINT NOT NULL DEFAULT 0, -- 0/1
  created_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT wallets_currency_chk CHECK (currency ~ '^[A-Z]{3}$'),
  CONSTRAINT wallets_is_default_chk CHECK (is_default IN (0,1))
);
CREATE INDEX IF NOT EXISTS idx_user_wallets_user ON user_wallets(user_id);
CREATE INDEX IF NOT EXISTS idx_user_wallets_provider ON user_wallets(provider);
CREATE TRIGGER trg_user_wallets_updated_at
BEFORE UPDATE ON user_wallets FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- user_addresses
CREATE TABLE IF NOT EXISTS user_addresses (
  id         BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  user_id    BIGINT NOT NULL,
  name       VARCHAR(150) NOT NULL,
  phone      VARCHAR(20)  NOT NULL,
  address    VARCHAR(255) NOT NULL,
  is_default SMALLINT NOT NULL DEFAULT 0,
  status     SMALLINT NOT NULL DEFAULT 1,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT addr_is_default_chk CHECK (is_default IN (0,1)),
  CONSTRAINT addr_status_chk     CHECK (status IN (0,1))
);
CREATE INDEX IF NOT EXISTS idx_user_addresses_user ON user_addresses(user_id);
CREATE TRIGGER trg_user_addresses_updated_at
BEFORE UPDATE ON user_addresses FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- categories
CREATE TABLE IF NOT EXISTS categories (
  id          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  name        VARCHAR(120) NOT NULL UNIQUE,
  slug        VARCHAR(150) NOT NULL UNIQUE,
  description TEXT,
  status      SMALLINT NOT NULL DEFAULT 1,
  created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT categories_status_chk CHECK (status IN (0,1))
);
CREATE TRIGGER trg_categories_updated_at
BEFORE UPDATE ON categories FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- products
CREATE TABLE IF NOT EXISTS products (
  id           BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  category_id  BIGINT NOT NULL,
  name         VARCHAR(150) NOT NULL,
  slug         VARCHAR(180) NOT NULL UNIQUE,
  images       JSONB NOT NULL,
  description  TEXT,
  price        INTEGER NOT NULL DEFAULT 0,
  status       SMALLINT NOT NULL DEFAULT 1, -- 1=active,0=hidden
  created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT products_status_chk CHECK (status IN (0,1)),
  CONSTRAINT products_price_chk  CHECK (price >= 0)
);
CREATE INDEX IF NOT EXISTS idx_products_category ON products(category_id);
CREATE INDEX IF NOT EXISTS idx_products_status ON products(status);
CREATE TRIGGER trg_products_updated_at
BEFORE UPDATE ON products FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- product_options
CREATE TABLE IF NOT EXISTS product_options (
  id         BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  product_id BIGINT NOT NULL,
  name       VARCHAR(80)  NOT NULL, -- size/crust/topping
  type       VARCHAR(50),           -- Size, Topping, Combo,...
  price      INTEGER NOT NULL DEFAULT 0, -- delta vs base
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT product_options_price_chk CHECK (price >= 0)
);
CREATE INDEX IF NOT EXISTS idx_product_options_product ON product_options(product_id);
CREATE TRIGGER trg_product_options_updated_at
BEFORE UPDATE ON product_options FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- carts
CREATE TABLE IF NOT EXISTS carts (
  id         BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  user_id    BIGINT,
  cart_item  JSONB NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_carts_user ON carts(user_id);

-- orders
CREATE TABLE IF NOT EXISTS orders (
  id                BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  code              VARCHAR(20) NOT NULL UNIQUE,
  user_id           BIGINT,
  coupons_id        BIGINT,
  ship_name         VARCHAR(150) NOT NULL,
  ship_phone        VARCHAR(20)  NOT NULL,
  ship_address_text VARCHAR(255) NOT NULL,
  price_subtotal    INTEGER NOT NULL DEFAULT 0,
  price_discount    INTEGER NOT NULL DEFAULT 0,
  price_shipping    INTEGER NOT NULL DEFAULT 0,
  total_price       INTEGER NOT NULL DEFAULT 0,
  payment_status    SMALLINT NOT NULL DEFAULT 0, -- 0=unpaid,1=paid,2=refunded
  status            SMALLINT NOT NULL DEFAULT 0, -- 0..4
  note              VARCHAR(255),
  created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT orders_payment_status_chk CHECK (payment_status IN (0,1,2)),
  CONSTRAINT orders_status_chk CHECK (status IN (0,1,2,3,4)),
  CONSTRAINT orders_nonneg_prices_chk CHECK (
    price_subtotal >= 0 AND price_discount >= 0 AND price_shipping >= 0 AND total_price >= 0
  )
);
CREATE INDEX IF NOT EXISTS idx_orders_user   ON orders(user_id);
CREATE INDEX IF NOT EXISTS idx_orders_status ON orders(status);
CREATE TRIGGER trg_orders_updated_at
BEFORE UPDATE ON orders FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- order_items
CREATE TABLE IF NOT EXISTS order_items (
  id         BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  order_id   BIGINT NOT NULL,
  product_id BIGINT NOT NULL,
  quantity   INTEGER NOT NULL,
  price      INTEGER NOT NULL,
  total      INTEGER NOT NULL,
  options    JSONB,
  CONSTRAINT order_items_qty_chk   CHECK (quantity > 0),
  CONSTRAINT order_items_price_chk CHECK (price   >= 0),
  CONSTRAINT order_items_total_chk CHECK (total   >= 0)
);
CREATE INDEX IF NOT EXISTS idx_order_items_order ON order_items(order_id);
CREATE INDEX IF NOT EXISTS idx_order_items_prod  ON order_items(product_id);

-- order_payments
CREATE TABLE IF NOT EXISTS order_payments (
  id         BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  order_id   BIGINT NOT NULL,
  amount     INTEGER NOT NULL,
  currency   VARCHAR(10) NOT NULL DEFAULT 'VND',
  status     SMALLINT NOT NULL DEFAULT 0, -- 0=pending,1=success,2=failed
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT order_payments_amount_chk CHECK (amount >= 0),
  CONSTRAINT order_payments_status_chk CHECK (status IN (0,1,2))
);
CREATE INDEX IF NOT EXISTS idx_order_payments_order ON order_payments(order_id);

-- coupons
CREATE TABLE IF NOT EXISTS coupons (
  id          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  product_id  BIGINT,
  code        VARCHAR(30)  NOT NULL UNIQUE,
  name        VARCHAR(150) NOT NULL,
  type        SMALLINT     NOT NULL,    -- 1: %, 2: fixed
  value       INTEGER      NOT NULL,    -- percent or fixed amount
  start_at    TIMESTAMPTZ  NOT NULL,
  end_at      TIMESTAMPTZ  NOT NULL,
  description TEXT,
  total       INTEGER NOT NULL DEFAULT 0,
  used_count  INTEGER NOT NULL DEFAULT 0,
  status      SMALLINT NOT NULL DEFAULT 1,
  created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT coupons_type_chk  CHECK (type IN (1,2)),
  CONSTRAINT coupons_range_chk CHECK (end_at > start_at),
  CONSTRAINT coupons_counts_chk CHECK (total >= 0 AND used_count >= 0),
  CONSTRAINT coupons_status_chk CHECK (status IN (0,1))
);
CREATE INDEX IF NOT EXISTS idx_coupons_product ON coupons(product_id);
CREATE INDEX IF NOT EXISTS idx_coupons_status  ON coupons(status);
CREATE TRIGGER trg_coupons_updated_at
BEFORE UPDATE ON coupons FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- product_reviews
CREATE TABLE IF NOT EXISTS product_reviews (
  id         BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  product_id BIGINT NOT NULL,
  user_id    BIGINT NOT NULL,
  rating     SMALLINT NOT NULL, -- 1–5
  comment    TEXT,
  status     SMALLINT NOT NULL DEFAULT 1, -- 1=active,0=ban
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT product_reviews_rating_chk CHECK (rating BETWEEN 1 AND 5),
  CONSTRAINT product_reviews_status_chk CHECK (status IN (0,1))
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_reviews_product_user ON product_reviews(product_id, user_id);
CREATE INDEX IF NOT EXISTS idx_reviews_product ON product_reviews(product_id);
CREATE INDEX IF NOT EXISTS idx_reviews_user    ON product_reviews(user_id);
CREATE TRIGGER trg_product_reviews_updated_at
BEFORE UPDATE ON product_reviews FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- banners
CREATE TABLE IF NOT EXISTS banners (
  id         BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  name       VARCHAR(120) NOT NULL,
  image      VARCHAR(255) NOT NULL,
  link       VARCHAR(255),
  status     SMALLINT NOT NULL DEFAULT 1, -- 1=active,0=inactive
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT banners_status_chk CHECK (status IN (0,1))
);
CREATE INDEX IF NOT EXISTS idx_banners_status ON banners(status);
CREATE TRIGGER trg_banners_updated_at
BEFORE UPDATE ON banners FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- reports_daily
CREATE TABLE IF NOT EXISTS reports_daily (
  id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  report_date     DATE NOT NULL UNIQUE,
  orders_count    INTEGER NOT NULL DEFAULT 0,
  revenue_total   INTEGER NOT NULL DEFAULT 0,
  customers_count INTEGER NOT NULL DEFAULT 0,
  status          SMALLINT NOT NULL DEFAULT 1,
  created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT reports_daily_nonneg_chk CHECK (orders_count >= 0 AND revenue_total >= 0 AND customers_count >= 0),
  CONSTRAINT reports_daily_status_chk CHECK (status IN (0,1))
);
CREATE TRIGGER trg_reports_daily_updated_at
BEFORE UPDATE ON reports_daily FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- =================================
-- 2) ADD FOREIGN KEYS (AFTER ALL)
-- =================================

ALTER TABLE user_wallets
  ADD CONSTRAINT fk_user_wallets_user
  FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE;

ALTER TABLE user_addresses
  ADD CONSTRAINT fk_user_addresses_user
  FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE;

ALTER TABLE products
  ADD CONSTRAINT fk_products_category
  FOREIGN KEY (category_id) REFERENCES categories(id) ON DELETE RESTRICT;

ALTER TABLE product_options
  ADD CONSTRAINT fk_product_options_product
  FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE CASCADE;

ALTER TABLE carts
  ADD CONSTRAINT fk_carts_user
  FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL;

ALTER TABLE orders
  ADD CONSTRAINT fk_orders_user
  FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL;

ALTER TABLE coupons
  ADD CONSTRAINT fk_coupons_product
  FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE SET NULL;

ALTER TABLE orders
  ADD CONSTRAINT fk_orders_coupon
  FOREIGN KEY (coupons_id) REFERENCES coupons(id) ON DELETE SET NULL;

ALTER TABLE order_items
  ADD CONSTRAINT fk_order_items_order
  FOREIGN KEY (order_id) REFERENCES orders(id) ON DELETE CASCADE,
  ADD CONSTRAINT fk_order_items_product
  FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE RESTRICT;

ALTER TABLE order_payments
  ADD CONSTRAINT fk_order_payments_order
  FOREIGN KEY (order_id) REFERENCES orders(id) ON DELETE CASCADE;

ALTER TABLE product_reviews
  ADD CONSTRAINT fk_reviews_product
  FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE CASCADE,
  ADD CONSTRAINT fk_reviews_user
  FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE;

COMMIT;

Dữ liệu mẫu category product product_option counpons


-- 1) CATEGORIES (10 danh mục)
INSERT INTO categories (name, slug, description)
VALUES
  ('Combo bữa ăn', 'combo-bua-an', 'Gồm burger + nước + khoai'),
  ('Burger', 'burger', 'Bánh mì kẹp thịt, phô mai, rau'),
  ('Gà rán', 'ga-ran', 'Các món gà giòn cay, gà sốt'),
  ('Mì & Cơm', 'mi-com', 'Các món cơm chiên, mì Ý'),
  ('Thức uống', 'thuc-uong', 'Nước ngọt, trà sữa, cà phê'),
  ('Tráng miệng', 'trang-mieng', 'Kem, bánh ngọt, pudding'),
  -- 4 danh mục mở rộng để đủ 10
  ('Pizza', 'pizza', 'Pizza đế mỏng/đế dày, nhiều topping'),
  ('Salad & Ăn nhẹ', 'salad-an-nhe', 'Salad, khoai, ngô, súp…'),
  ('Đồ chay', 'do-chay', 'Thực đơn chay thân thiện'),
  ('Bữa sáng', 'bua-sang', 'Set breakfast, sandwich, cà phê sáng')
ON CONFLICT (slug) DO UPDATE
SET name = EXCLUDED.name,
    description = EXCLUDED.description;

-- Helper: id theo slug (để tham chiếu an toàn)
-- (không cần tạo biến; dùng subquery trong từng INSERT product)

-- ================================
-- 2) PRODUCTS (6 danh mục đầu, mỗi danh mục 10 món = 60 món mẫu)
-- ================================
-- CATEGORY: Combo bữa ăn
INSERT INTO products (category_id, name, slug, images, description, price)
VALUES
((SELECT id FROM categories WHERE slug='combo-bua-an'),'Combo Burger Gà + Pepsi','combo-burger-ga-pepsi','["combo_ga.jpg"]'::jsonb,'1 burger gà + Pepsi + khoai tây',79000),
((SELECT id FROM categories WHERE slug='combo-bua-an'),'Combo Burger Bò + Khoai','combo-burger-bo-khoai','["combo_bo.jpg"]'::jsonb,'1 burger bò + khoai + nước',89000),
((SELECT id FROM categories WHERE slug='combo-bua-an'),'Combo Gà Rán (2 miếng) + Pepsi','combo-ga-ran-2m-pepsi','["combo_garan.jpg"]'::jsonb,'2 miếng gà + Pepsi + khoai',99000),
((SELECT id FROM categories WHERE slug='combo-bua-an'),'Combo Mì Ý Bò + 7Up','combo-mi-y-bo-7up','["combo_mi.jpg"]'::jsonb,'Mì Ý sốt bò bằm + 7Up',109000),
((SELECT id FROM categories WHERE slug='combo-bua-an'),'Combo Cơm Gà Nước Mắm','combo-com-ga-nuoc-mam','["combo_comga.jpg"]'::jsonb,'Cơm gà chiên nước mắm + nước ngọt',99000),
((SELECT id FROM categories WHERE slug='combo-bua-an'),'Combo Burger Tôm + Khoai','combo-burger-tom-khoai','["combo_tom.jpg"]'::jsonb,'Burger tôm + khoai + nước',95000),
((SELECT id FROM categories WHERE slug='combo-bua-an'),'Combo Double Burger Bò','combo-double-burger-bo','["combo_doublebo.jpg"]'::jsonb,'Burger bò 2 lớp + khoai + nước',129000),
((SELECT id FROM categories WHERE slug='combo-bua-an'),'Combo Gà Cay Hàn + Khoai','combo-ga-cay-han-khoai','["combo_gacay.jpg"]'::jsonb,'Gà cay Hàn + khoai + nước',119000),
((SELECT id FROM categories WHERE slug='combo-bua-an'),'Combo Cánh Gà + Pepsi','combo-canh-ga-pepsi','["combo_canhga.jpg"]'::jsonb,'4 cánh gà mật ong + Pepsi',105000),
((SELECT id FROM categories WHERE slug='combo-bua-an'),'Combo Gia Đình 2 Người','combo-gia-dinh-2nguoi','["combo_family2.jpg"]'::jsonb,'2 burger + 1 khoai lớn + 2 nước',159000)
ON CONFLICT (slug) DO UPDATE
SET name=EXCLUDED.name, images=EXCLUDED.images, description=EXCLUDED.description, price=EXCLUDED.price;

-- CATEGORY: Burger
INSERT INTO products (category_id, name, slug, images, description, price)
VALUES
((SELECT id FROM categories WHERE slug='burger'),'Burger Bò Phô Mai','burger-bo-pho-mai','["burger_bo.jpg"]'::jsonb,'Bò + phô mai tan chảy',59000),
((SELECT id FROM categories WHERE slug='burger'),'Burger Gà Giòn Cay','burger-ga-gion-cay','["burger_ga.jpg"]'::jsonb,'Gà giòn cay, rau xà lách, mayo',56000),
((SELECT id FROM categories WHERE slug='burger'),'Burger Tôm','burger-tom','["burger_tom.jpg"]'::jsonb,'Tôm chiên giòn + sốt tartar',62000),
((SELECT id FROM categories WHERE slug='burger'),'Burger Bò BBQ','burger-bo-bbq','["burger_bbbq.jpg"]'::jsonb,'Thịt bò sốt BBQ + hành phi',65000),
((SELECT id FROM categories WHERE slug='burger'),'Burger Gà Teriyaki','burger-ga-teriyaki','["burger_gter.jpg"]'::jsonb,'Gà sốt Nhật + rau',62000),
((SELECT id FROM categories WHERE slug='burger'),'Burger Cá Fillet','burger-ca-fillet','["burger_ca.jpg"]'::jsonb,'Cá fillet chiên + sốt tartar',59000),
((SELECT id FROM categories WHERE slug='burger'),'Burger Bò Trứng','burger-bo-trung','["burger_botrung.jpg"]'::jsonb,'Bò + trứng ốp la + phô mai',64000),
((SELECT id FROM categories WHERE slug='burger'),'Burger Nấm Phô Mai','burger-nam-pho-mai','["burger_nam.jpg"]'::jsonb,'Nấm xào bơ + phô mai',63000),
((SELECT id FROM categories WHERE slug='burger'),'Burger Bò Pepper','burger-bo-pepper','["burger_pepper.jpg"]'::jsonb,'Bò sốt tiêu đen',67000),
((SELECT id FROM categories WHERE slug='burger'),'Double Cheese Burger','double-cheese-burger','["burger_double.jpg"]'::jsonb,'2 lát phô mai + bò',72000)
ON CONFLICT (slug) DO UPDATE
SET name=EXCLUDED.name, images=EXCLUDED.images, description=EXCLUDED.description, price=EXCLUDED.price;

-- CATEGORY: Gà rán
INSERT INTO products (category_id, name, slug, images, description, price)
VALUES
((SELECT id FROM categories WHERE slug='ga-ran'),'Gà Rán Truyền Thống (2 miếng)','ga-ran-truyen-thong-2','["ga_ran_2mieng.jpg"]'::jsonb,'Gà rán giòn thơm',69000),
((SELECT id FROM categories WHERE slug='ga-ran'),'Gà Cay Hàn Quốc (2 miếng)','ga-cay-han-quoc-2','["ga_cay.jpg"]'::jsonb,'Gà sốt cay đậm vị',72000),
((SELECT id FROM categories WHERE slug='ga-ran'),'Cánh Gà Sốt Mật Ong (4 cánh)','canh-ga-mat-ong-4','["canh_ga_matong.jpg"]'::jsonb,'Cánh gà phủ mật ong',65000),
((SELECT id FROM categories WHERE slug='ga-ran'),'Popcorn Chicken','popcorn-chicken','["popcorn_chicken.jpg"]'::jsonb,'Gà viên chiên giòn',45000),
((SELECT id FROM categories WHERE slug='ga-ran'),'Gà Không Xương Sốt Cay','ga-khong-xuong-sot-cay','["ga_kx_cay.jpg"]'::jsonb,'Gà không xương sốt cay',69000),
((SELECT id FROM categories WHERE slug='ga-ran'),'Gà Rán Tỏi','ga-ran-toi','["ga_ran_toi.jpg"]'::jsonb,'Gà rán vị tỏi',70000),
((SELECT id FROM categories WHERE slug='ga-ran'),'Gà Sốt Phô Mai','ga-sot-pho-mai','["ga_phomai.jpg"]'::jsonb,'Gà phủ phô mai kéo sợi',78000),
((SELECT id FROM categories WHERE slug='ga-ran'),'Cánh Gà Buffalo','canh-ga-buffalo','["ga_buffalo.jpg"]'::jsonb,'Cánh gà sốt buffalo cay',71000),
((SELECT id FROM categories WHERE slug='ga-ran'),'Đùi Gà Giòn','dui-ga-gion','["ga_duigion.jpg"]'::jsonb,'Đùi gà giòn rụm',52000),
((SELECT id FROM categories WHERE slug='ga-ran'),'Gà Sốt Chanh Dây','ga-sot-chanh-day','["ga_chanhday.jpg"]'::jsonb,'Vị chua ngọt lạ miệng',73000)
ON CONFLICT (slug) DO UPDATE
SET name=EXCLUDED.name, images=EXCLUDED.images, description=EXCLUDED.description, price=EXCLUDED.price;

-- CATEGORY: Mì & Cơm
INSERT INTO products (category_id, name, slug, images, description, price)
VALUES
((SELECT id FROM categories WHERE slug='mi-com'),'Cơm Gà Chiên Nước Mắm','com-ga-nuoc-mam','["com_ga_nuocmam.jpg"]'::jsonb,'Cơm + gà chiên nước mắm',69000),
((SELECT id FROM categories WHERE slug='mi-com'),'Cơm Gà Teriyaki','com-ga-teriyaki','["com_ga_teriyaki.jpg"]'::jsonb,'Cơm + gà sốt Nhật',72000),
((SELECT id FROM categories WHERE slug='mi-com'),'Mì Ý Bò Bằm','mi-y-bo-bam','["mi_y_bo_bam.jpg"]'::jsonb,'Pasta + sốt bò bằm',75000),
((SELECT id FROM categories WHERE slug='mi-com'),'Mì Ý Sốt Kem Gà','mi-y-sot-kem-ga','["mi_y_kem.jpg"]'::jsonb,'Pasta + sốt kem',72000),
((SELECT id FROM categories WHERE slug='mi-com'),'Cơm Chiên Dương Châu','com-chien-duong-chau','["com_duongchau.jpg"]'::jsonb,'Cơm chiên thập cẩm',59000),
((SELECT id FROM categories WHERE slug='mi-com'),'Cơm Gà Sốt BBQ','com-ga-sot-bbq','["com_bbq.jpg"]'::jsonb,'Cơm + gà sốt BBQ',71000),
((SELECT id FROM categories WHERE slug='mi-com'),'Mì Ý Hải Sản','mi-y-hai-san','["mi_hai_san.jpg"]'::jsonb,'Pasta + hải sản',82000),
((SELECT id FROM categories WHERE slug='mi-com'),'Cơm Bò Lúc Lắc','com-bo-luc-lac','["com_luclac.jpg"]'::jsonb,'Bò lúc lắc + cơm',89000),
((SELECT id FROM categories WHERE slug='mi-com'),'Cơm Sườn Nướng','com-suon-nuong','["com_suon.jpg"]'::jsonb,'Sườn nướng + cơm',86000),
((SELECT id FROM categories WHERE slug='mi-com'),'Cơm Gà Xối Mỡ','com-ga-xoi-mo','["com_xoimo.jpg"]'::jsonb,'Gà xối mỡ + cơm',68000)
ON CONFLICT (slug) DO UPDATE
SET name=EXCLUDED.name, images=EXCLUDED.images, description=EXCLUDED.description, price=EXCLUDED.price;

-- CATEGORY: Thức uống
INSERT INTO products (category_id, name, slug, images, description, price)
VALUES
((SELECT id FROM categories WHERE slug='thuc-uong'),'Pepsi (500ml)','pepsi-500','["pepsi.jpg"]'::jsonb,'Ly 500ml',19000),
((SELECT id FROM categories WHERE slug='thuc-uong'),'7Up (500ml)','7up-500','["7up.jpg"]'::jsonb,'Ly 500ml',19000),
((SELECT id FROM categories WHERE slug='thuc-uong'),'Trà Đào Cam Sả','tra-dao-cam-sa','["tra_dao.jpg"]'::jsonb,'Trà đào tươi, thơm dịu',29000),
((SELECT id FROM categories WHERE slug='thuc-uong'),'Cà Phê Sữa Đá','ca-phe-sua-da','["cf_suada.jpg"]'::jsonb,'Cà phê Việt Nam chuẩn vị',25000),
((SELECT id FROM categories WHERE slug='thuc-uong'),'Trà Sữa Trân Châu','tra-sua-tran-chau','["trasua_tc.jpg"]'::jsonb,'Trà sữa vị truyền thống',32000),
((SELECT id FROM categories WHERE slug='thuc-uong'),'Nước Chanh Tươi','nuoc-chanh-tuoi','["chanh_tuoi.jpg"]'::jsonb,'Chanh tươi giải khát',22000),
((SELECT id FROM categories WHERE slug='thuc-uong'),'Trà Chanh Sả','tra-chanh-sa','["tra_chanhsa.jpg"]'::jsonb,'Trà chanh sả mát',25000),
((SELECT id FROM categories WHERE slug='thuc-uong'),'Sprite (500ml)','sprite-500','["sprite.jpg"]'::jsonb,'Ly 500ml',19000),
((SELECT id FROM categories WHERE slug='thuc-uong'),'Coca-Cola (500ml)','coca-500','["coca.jpg"]'::jsonb,'Ly 500ml',19000),
((SELECT id FROM categories WHERE slug='thuc-uong'),'Sữa Tươi Trân Châu Đường Đen','sua-tuoi-tcdd','["suatuoi_tcdd.jpg"]'::jsonb,'Ngọt đậm đà',35000)
ON CONFLICT (slug) DO UPDATE
SET name=EXCLUDED.name, images=EXCLUDED.images, description=EXCLUDED.description, price=EXCLUDED.price;

-- CATEGORY: Tráng miệng
INSERT INTO products (category_id, name, slug, images, description, price)
VALUES
((SELECT id FROM categories WHERE slug='trang-mieng'),'Kem Ly Vanilla','kem-ly-vanilla','["kem_vanilla.jpg"]'::jsonb,'Kem mịn, ngọt dịu',22000),
((SELECT id FROM categories WHERE slug='trang-mieng'),'Kem Socola','kem-socola','["kem_socola.jpg"]'::jsonb,'Vị socola đậm đà',23000),
((SELECT id FROM categories WHERE slug='trang-mieng'),'Bánh Brownie','banh-brownie','["brownie.jpg"]'::jsonb,'Bánh socola mềm ẩm',30000),
((SELECT id FROM categories WHERE slug='trang-mieng'),'Pudding Trứng','pudding-trung','["pudding.jpg"]'::jsonb,'Pudding mềm mịn',27000),
((SELECT id FROM categories WHERE slug='trang-mieng'),'Bánh Flan Caramel','banh-flan-caramel','["flan.jpg"]'::jsonb,'Flan caramel béo mịn',26000),
((SELECT id FROM categories WHERE slug='trang-mieng'),'Tiramisu Miếng','tiramisu-mieng','["tiramisu.jpg"]'::jsonb,'Béo, thơm cà phê',39000),
((SELECT id FROM categories WHERE slug='trang-mieng'),'Cheesecake Dâu','cheesecake-dau','["cheesecake.jpg"]'::jsonb,'Cheesecake vị dâu',42000),
((SELECT id FROM categories WHERE slug='trang-mieng'),'Bánh Su Kem','banh-su-kem','["sukem.jpg"]'::jsonb,'Su kem tươi',28000),
((SELECT id FROM categories WHERE slug='trang-mieng'),'Kem Dừa Dằm','kem-dua-dam','["kem_dua.jpg"]'::jsonb,'Kem dừa & topping',32000),
((SELECT id FROM categories WHERE slug='trang-mieng'),'Trái Cây Mix Cup','trai-cay-mix-cup','["traicay.jpg"]'::jsonb,'Cốc trái cây tươi',35000)
ON CONFLICT (slug) DO UPDATE
SET name=EXCLUDED.name, images=EXCLUDED.images, description=EXCLUDED.description, price=EXCLUDED.price;

-- ================================
-- 3) PRODUCT OPTIONS (mẫu phổ biến)
-- ================================
-- Burger size & extra
INSERT INTO product_options (product_id, name, type, price)
VALUES
((SELECT id FROM products WHERE slug='burger-bo-pho-mai'),'Size','Size',0),
((SELECT id FROM products WHERE slug='burger-bo-pho-mai'),'Size Vừa','Size',5000),
((SELECT id FROM products WHERE slug='burger-bo-pho-mai'),'Size Lớn','Size',10000),
((SELECT id FROM products WHERE slug='burger-ga-gion-cay'),'Thêm phô mai','Extra',8000),
((SELECT id FROM products WHERE slug='burger-ga-gion-cay'),'Thêm sốt cay','Extra',5000),

-- Drinks size/sugar
((SELECT id FROM products WHERE slug='pepsi-500'),'Size Nhỏ','Size',0),
((SELECT id FROM products WHERE slug='pepsi-500'),'Size Lớn','Size',5000),
((SELECT id FROM products WHERE slug='tra-sua-tran-chau'),'Đường 0%','Sugar',0),
((SELECT id FROM products WHERE slug='tra-sua-tran-chau'),'Đường 50%','Sugar',0),
((SELECT id FROM products WHERE slug='tra-sua-tran-chau'),'Đường 100%','Sugar',0),
((SELECT id FROM products WHERE slug='tra-sua-tran-chau'),'Trân châu thêm','Topping',7000),

-- Gà rán sauces
((SELECT id FROM products WHERE slug='ga-ran-truyen-thong-2'),'Sốt chấm Tương ớt','Sauce',0),
((SELECT id FROM products WHERE slug='ga-ran-truyen-thong-2'),'Sốt Mayonnaise','Sauce',0),

-- Dessert extra
((SELECT id FROM products WHERE slug='banh-brownie'),'Thêm kem vanilla','Extra',10000)
ON CONFLICT DO NOTHING;

-- ================================
-- 4) COUPONS (mã % và tiền cứng)
-- type: 1 = %, 2 = tiền cứng (VND)
-- ================================
-- Window áp dụng: hôm nay +/- 180 ngày
WITH t AS (
  SELECT NOW() AS nowt
)
INSERT INTO coupons (product_id, code, name, type, value, start_at, end_at, description, total, used_count, status)
VALUES
(NULL,'WELCOME10','Giảm 10% đơn đầu',1,10, (SELECT nowt - INTERVAL '180 days' FROM t),(SELECT nowt + INTERVAL '180 days' FROM t),'Áp cho toàn bộ đơn, tối đa 50k',1000,0,1),
(NULL,'FREESHIP20K','Giảm 20k phí ship',2,20000,(SELECT nowt - INTERVAL '60 days' FROM t),(SELECT nowt + INTERVAL '120 days' FROM t),'Giảm phí vận chuyển',500,0,1),
((SELECT id FROM products WHERE slug='burger-bo-pho-mai'),'BO10K','Burger bò -10k',2,10000,(SELECT nowt - INTERVAL '30 days' FROM t),(SELECT nowt + INTERVAL '90 days' FROM t),'Giảm trực tiếp 10k cho Burger Bò Phô Mai',300,0,1),
((SELECT id FROM products WHERE slug='ga-cay-han-quoc-2'),'GA15','Gà cay -15%',1,15,(SELECT nowt - INTERVAL '15 days' FROM t),(SELECT nowt + INTERVAL '60 days' FROM t),'Áp riêng Gà cay Hàn',200,0,1),
(NULL,'LUNCH30','Trưa vui -30%',1,30,(SELECT date_trunc('day', nowt) + INTERVAL '11 hours' FROM t),(SELECT nowt + INTERVAL '120 days' FROM t),'Khung giờ 11h-14h, tối đa 40k',1000,0,1),
(NULL,'DRINK5K','Nước ngọt -5k',2,5000,(SELECT nowt - INTERVAL '7 days' FROM t),(SELECT nowt + INTERVAL '90 days' FROM t),'Áp các đồ uống',800,0,1),
((SELECT id FROM products WHERE slug='mi-y-bo-bam'),'PASTA7','Mì Ý -7%',1,7,(SELECT nowt - INTERVAL '90 days' FROM t),(SELECT nowt + INTERVAL '90 days' FROM t),'Giảm cho dòng pasta',400,0,1),
((SELECT id FROM products WHERE slug='com-ga-nuoc-mam'),'COM5K','Cơm gà -5k',2,5000,(SELECT nowt - INTERVAL '30 days' FROM t),(SELECT nowt + INTERVAL '120 days' FROM t),'Áp riêng cơm gà nước mắm',300,0,1),
(NULL,'SWEET15','Tráng miệng -15%',1,15,(SELECT nowt - INTERVAL '10 days' FROM t),(SELECT nowt + INTERVAL '100 days' FROM t),'Áp cho tráng miệng',600,0,1),
(NULL,'COMBO20','Combo -20%',1,20,(SELECT nowt - INTERVAL '180 days' FROM t),(SELECT nowt + INTERVAL '180 days' FROM t),'Áp các combo',1000,0,1),
((SELECT id FROM products WHERE slug='tra-sua-tran-chau'),'MILKTEA6K','Trà sữa -6k',2,6000,(SELECT nowt - INTERVAL '5 days' FROM t),(SELECT nowt + INTERVAL '80 days' FROM t),'Áp riêng trà sữa',500,0,1),
((SELECT id FROM products WHERE slug='pepsi-500'),'PEPSI3K','Pepsi -3k',2,3000,(SELECT nowt - INTERVAL '5 days' FROM t),(SELECT nowt + INTERVAL '80 days' FROM t),'Áp riêng Pepsi',500,0,1)
ON CONFLICT (code) DO UPDATE
SET name=EXCLUDED.name, type=EXCLUDED.type, value=EXCLUDED.value,
    start_at=EXCLUDED.start_at, end_at=EXCLUDED.end_at,
    description=EXCLUDED.description, total=EXCLUDED.total, status=EXCLUDED.status;

COMMIT;
