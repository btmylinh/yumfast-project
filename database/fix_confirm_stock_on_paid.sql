-- Fix ambiguous column reference in confirm_stock_on_paid function
-- Lỗi: column reference "quantity" is ambiguous
-- Sửa: thêm prefix ps. cho các cột của bảng product_stock

CREATE OR REPLACE FUNCTION public.confirm_stock_on_paid() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF NEW.status IN (2,3) AND OLD.status <> NEW.status THEN
        UPDATE public.product_stock ps
        SET 
            quantity = ps.quantity - oi.quantity,
            reserved = ps.reserved - oi.quantity
        FROM public.order_items oi
        WHERE oi.order_id = NEW.id
          AND oi.product_id = ps.product_id;
    END IF;

    RETURN NEW;
END;
$$;

ALTER FUNCTION public.confirm_stock_on_paid() OWNER TO postgres;

