using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebApp.Services;

namespace WebApp.Controllers
{
    public class BaseController : Controller
    {
        protected readonly IJsonLocalizationService _localizationService;

        public BaseController(IJsonLocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Set all common localized strings in ViewBag
            SetLocalizedStrings();
            base.OnActionExecuting(context);
        }

        private void SetLocalizedStrings()
        {
            // Common action strings - Strings hành động chung
            ViewBag.Add = _localizationService.GetLocalizedString("add");
            ViewBag.Edit = _localizationService.GetLocalizedString("edit");
            ViewBag.Delete = _localizationService.GetLocalizedString("delete");
            ViewBag.Save = _localizationService.GetLocalizedString("save");
            ViewBag.Update = _localizationService.GetLocalizedString("update");
            ViewBag.Cancel = _localizationService.GetLocalizedString("cancel");
            ViewBag.Create = _localizationService.GetLocalizedString("create");
            ViewBag.View = _localizationService.GetLocalizedString("view");
            ViewBag.Search = _localizationService.GetLocalizedString("search");
            ViewBag.Action = _localizationService.GetLocalizedString("action");
            ViewBag.Actions = _localizationService.GetLocalizedString("actions");
            ViewBag.Status = _localizationService.GetLocalizedString("status");
            ViewBag.Active = _localizationService.GetLocalizedString("active");
            ViewBag.Inactive = _localizationService.GetLocalizedString("inactive");
            ViewBag.Published = _localizationService.GetLocalizedString("published");
            ViewBag.Unpublished = _localizationService.GetLocalizedString("unpublished");
            ViewBag.Name = _localizationService.GetLocalizedString("name");
            ViewBag.Description = _localizationService.GetLocalizedString("description");
            ViewBag.Image = _localizationService.GetLocalizedString("image");
            ViewBag.CreatedAt = _localizationService.GetLocalizedString("created_at");
            ViewBag.UpdatedAt = _localizationService.GetLocalizedString("updated_at");

            // Common navigation and UI strings
            ViewBag.SearchForProducts = _localizationService.GetLocalizedString("searchForProducts");
            ViewBag.Support = _localizationService.GetLocalizedString("support");
            ViewBag.Delivery = _localizationService.GetLocalizedString("delivery");
            ViewBag.Warranty = _localizationService.GetLocalizedString("warranty");
            ViewBag.Notification = _localizationService.GetLocalizedString("notification");
            ViewBag.Account = _localizationService.GetLocalizedString("account");
            ViewBag.SignIn = _localizationService.GetLocalizedString("signIn");
            ViewBag.SignUp = _localizationService.GetLocalizedString("signUp");
            ViewBag.Profile = _localizationService.GetLocalizedString("profile");
            ViewBag.Settings = _localizationService.GetLocalizedString("settings");
            ViewBag.SignOut = _localizationService.GetLocalizedString("signOut");
            ViewBag.MyOrders = _localizationService.GetLocalizedString("myOrders");
            ViewBag.ShoppingCart = _localizationService.GetLocalizedString("shoppingCart");
            ViewBag.AllCategories = _localizationService.GetLocalizedString("allCategories");
            ViewBag.Home = _localizationService.GetLocalizedString("home");
            ViewBag.Shop = _localizationService.GetLocalizedString("shop");
            ViewBag.Vietnamese = _localizationService.GetLocalizedString("vietnamese");
            ViewBag.English = _localizationService.GetLocalizedString("english");
            ViewBag.Product = _localizationService.GetLocalizedString("product");

            // Dashboard strings
            ViewBag.Dashboard = _localizationService.GetLocalizedString("dashboard");
            ViewBag.Products = _localizationService.GetLocalizedString("products");
            ViewBag.Categories = _localizationService.GetLocalizedString("categories");
            ViewBag.Coupons = _localizationService.GetLocalizedString("coupons");
            ViewBag.List = _localizationService.GetLocalizedString("list");
            ViewBag.AddNew = _localizationService.GetLocalizedString("addNew");

            ViewBag.CouponRemaining = _localizationService.GetLocalizedString("couponRemaining");
            ViewBag.CouponNoData = _localizationService.GetLocalizedString("couponNoData");
            ViewBag.Copy = _localizationService.GetLocalizedString("copy");
            ViewBag.Copied = _localizationService.GetLocalizedString("copied");

            // Shop/Product strings
            ViewBag.ShopNow = _localizationService.GetLocalizedString("shopNow");
            ViewBag.AddToCart = _localizationService.GetLocalizedString("addToCart");
            ViewBag.Quantity = _localizationService.GetLocalizedString("quantity");

            // UI misc labels for JS/templates
            ViewBag.QuickView = _localizationService.GetLocalizedString("quick_view");
            ViewBag.Wishlist = _localizationService.GetLocalizedString("wishlist");
            ViewBag.Compare = _localizationService.GetLocalizedString("compare");
            ViewBag.NewBadge = _localizationService.GetLocalizedString("new_badge");
            ViewBag.SaleBadge = _localizationService.GetLocalizedString("sale_badge");
            ViewBag.DealHurry = _localizationService.GetLocalizedString("deal_hurry");
            ViewBag.AlreadySold = _localizationService.GetLocalizedString("already_sold");
            ViewBag.Available = _localizationService.GetLocalizedString("available");

            // Home headings
            ViewBag.NewProducts = _localizationService.GetLocalizedString("new_products");
            ViewBag.BestSellingProducts = _localizationService.GetLocalizedString("best_selling_products");
            ViewBag.DealOfDay = _localizationService.GetLocalizedString("deal_of_day");
            ViewBag.ViewAll = _localizationService.GetLocalizedString("view_all");

            // Dashboard specific strings
            ViewBag.WelcomeBack = _localizationService.GetLocalizedString("welcomeBack");
            ViewBag.WelcomeDescription = _localizationService.GetLocalizedString("welcomeDescription");
            ViewBag.CreateProduct = _localizationService.GetLocalizedString("createProduct");
            ViewBag.Earnings = _localizationService.GetLocalizedString("earnings");
            ViewBag.MonthlyRevenue = _localizationService.GetLocalizedString("monthlyRevenue");
            ViewBag.Orders = _localizationService.GetLocalizedString("orders");
            ViewBag.NewSales = _localizationService.GetLocalizedString("newSales");
            ViewBag.User = _localizationService.GetLocalizedString("user");
            ViewBag.NewInDays = _localizationService.GetLocalizedString("newInDays");

            // Additional UI strings
            ViewBag.ShopCart = _localizationService.GetLocalizedString("shopCart");
            ViewBag.ContinueShopping = _localizationService.GetLocalizedString("continueShopping");
            ViewBag.UpdateCart = _localizationService.GetLocalizedString("updateCart");
            ViewBag.ShopCheckout = _localizationService.GetLocalizedString("shopCheckout");
            ViewBag.BecomeAShopper = _localizationService.GetLocalizedString("becomeAShopper");
            ViewBag.ShopperOpportunities = _localizationService.GetLocalizedString("shopperOpportunities");
            ViewBag.VndCurrency = _localizationService.GetLocalizedString("vndCurrency");
            ViewBag.UsdCurrency = _localizationService.GetLocalizedString("usdCurrency");
            ViewBag.IdeasAndGuides = _localizationService.GetLocalizedString("ideasAndGuides");
            ViewBag.NewRetailers = _localizationService.GetLocalizedString("newRetailers");

            // Dashboard specific strings
            ViewBag.DashboardTitle = _localizationService.GetLocalizedString("dashboardTitle");
            ViewBag.StoreManagements = _localizationService.GetLocalizedString("storeManagements");
            ViewBag.Users = _localizationService.GetLocalizedString("users");
            ViewBag.Reviews = _localizationService.GetLocalizedString("reviews");
            ViewBag.Reports = _localizationService.GetLocalizedString("reports");
            ViewBag.Inventory = _localizationService.GetLocalizedString("inventory");

            // Product page strings
            ViewBag.ProductTitle = _localizationService.GetLocalizedString("product_title");
            ViewBag.ProductAddProduct = _localizationService.GetLocalizedString("product_add_product");
            ViewBag.ProductStatus = ViewBag.Status; // Reuse common Status
            ViewBag.ProductActive = ViewBag.Active; // Reuse common Active
            ViewBag.ProductDeactive = ViewBag.Inactive; // Reuse common Inactive
            ViewBag.ProductDraft = _localizationService.GetLocalizedString("product_draft");
            ViewBag.ProductImage = ViewBag.Image; // Reuse common Image
            ViewBag.ProductName = ViewBag.Name; // Reuse common Name
            ViewBag.ProductCategory = _localizationService.GetLocalizedString("product_category");
            ViewBag.ProductPrice = _localizationService.GetLocalizedString("product_price");
            ViewBag.ProductCreateAt = ViewBag.CreatedAt; // Reuse common CreatedAt
            ViewBag.ProductEdit = ViewBag.Edit; // Reuse common Edit
            ViewBag.ProductDelete = ViewBag.Delete; // Reuse common Delete
            ViewBag.ProductSearch = ViewBag.Search; // Reuse common Search

            // Category page strings
            ViewBag.CategoryTitle = _localizationService.GetLocalizedString("category_title");
            ViewBag.CategoryAddCategory = _localizationService.GetLocalizedString("category_add_category");
            ViewBag.CategoryName = ViewBag.Name; // Reuse common Name
            ViewBag.CategoryDescription = ViewBag.Description; // Reuse common Description
            ViewBag.CategoryStatus = ViewBag.Status; // Reuse common Status
            ViewBag.CategoryActive = ViewBag.Active; // Reuse common Active
            ViewBag.CategoryInactive = ViewBag.Inactive; // Reuse common Inactive
            ViewBag.CategoryEdit = ViewBag.Edit; // Reuse common Edit
            ViewBag.CategoryDelete = ViewBag.Delete; // Reuse common Delete

            // Order page strings
            ViewBag.OrderTitle = _localizationService.GetLocalizedString("order_title");
            ViewBag.OrderId = _localizationService.GetLocalizedString("order_id");
            ViewBag.OrderDate = _localizationService.GetLocalizedString("order_date");
            ViewBag.OrderUser = _localizationService.GetLocalizedString("order_user");
            ViewBag.OrderStatus = ViewBag.Status; // Reuse common Status
            ViewBag.OrderTotal = _localizationService.GetLocalizedString("order_total");
            ViewBag.OrderPayment = _localizationService.GetLocalizedString("order_payment");
            ViewBag.OrderView = ViewBag.View; // Reuse common View
            ViewBag.OrderEdit = ViewBag.Edit; // Reuse common Edit
            ViewBag.OrderDelete = ViewBag.Delete; // Reuse common Delete
            ViewBag.OrderPending = _localizationService.GetLocalizedString("order_pending");
            ViewBag.OrderProcessing = _localizationService.GetLocalizedString("order_processing");
            ViewBag.OrderShipped = _localizationService.GetLocalizedString("order_shipped");
            ViewBag.OrderDelivered = _localizationService.GetLocalizedString("order_delivered");
            ViewBag.OrderCancelled = _localizationService.GetLocalizedString("order_cancelled");

            // User page strings
            ViewBag.UserTitle = _localizationService.GetLocalizedString("user_title");
            ViewBag.UserAddNew = _localizationService.GetLocalizedString("user_add_new");
            ViewBag.UserName = ViewBag.Name; // Reuse common Name
            ViewBag.UserEmail = _localizationService.GetLocalizedString("user_email");
            ViewBag.UserPhone = _localizationService.GetLocalizedString("user_phone");
            ViewBag.UserOrders = _localizationService.GetLocalizedString("user_orders");
            ViewBag.UserStatus = ViewBag.Status; // Reuse common Status
            ViewBag.UserEdit = ViewBag.Edit; // Reuse common Edit
            ViewBag.UserDelete = ViewBag.Delete; // Reuse common Delete
            ViewBag.UserView = ViewBag.View; // Reuse common View

            // Review page strings
            ViewBag.ReviewTitle = _localizationService.GetLocalizedString("review_title");
            ViewBag.ReviewProduct = _localizationService.GetLocalizedString("review_product");
            ViewBag.ReviewUser = _localizationService.GetLocalizedString("review_user");
            ViewBag.ReviewRating = _localizationService.GetLocalizedString("review_rating");
            ViewBag.ReviewComment = _localizationService.GetLocalizedString("review_comment");
            ViewBag.ReviewDate = _localizationService.GetLocalizedString("review_date");

            // Banner page strings
            ViewBag.Banners = _localizationService.GetLocalizedString("banners");
            ViewBag.BannerTitle = _localizationService.GetLocalizedString("banner_title");
            ViewBag.BannerAddBanner = _localizationService.GetLocalizedString("banner_add_banner");
            ViewBag.BannerCreate = ViewBag.Create; // Reuse common Create
            ViewBag.BannerEdit = ViewBag.Edit; // Reuse common Edit
            ViewBag.BannerName = ViewBag.Name; // Reuse common Name
            ViewBag.BannerDescription = ViewBag.Description; // Reuse common Description
            ViewBag.BannerImage = ViewBag.Image; // Reuse common Image
            ViewBag.BannerPosition = _localizationService.GetLocalizedString("banner_position");
            ViewBag.BannerStatus = ViewBag.Status; // Reuse common Status
            ViewBag.BannerOrder = _localizationService.GetLocalizedString("banner_order");
            ViewBag.BannerLink = _localizationService.GetLocalizedString("banner_link");
            ViewBag.BannerStartDate = _localizationService.GetLocalizedString("banner_start_date");
            ViewBag.BannerEndDate = _localizationService.GetLocalizedString("banner_end_date");
            ViewBag.BannerType = _localizationService.GetLocalizedString("banner_type");
            ViewBag.BannerActive = ViewBag.Active; // Reuse common Active
            ViewBag.BannerInactive = ViewBag.Inactive; // Reuse common Inactive
            ViewBag.BannerCreatedAt = ViewBag.CreatedAt; // Reuse common CreatedAt
            ViewBag.BannerEditAction = ViewBag.Edit; // Reuse common Edit
            ViewBag.BannerDeleteAction = ViewBag.Delete; // Reuse common Delete
            ViewBag.BannerToggleAction = _localizationService.GetLocalizedString("banner_toggle_action");
            ViewBag.BannerPositionHomepage = _localizationService.GetLocalizedString("banner_position_homepage");
            ViewBag.BannerPositionShop = _localizationService.GetLocalizedString("banner_position_shop");
            ViewBag.BannerPositionCategory = _localizationService.GetLocalizedString("banner_position_category");
            ViewBag.BannerPositionProduct = _localizationService.GetLocalizedString("banner_position_product");
            ViewBag.BannerSelectPosition = _localizationService.GetLocalizedString("banner_select_position");
            ViewBag.BannerSelectValidation = _localizationService.GetLocalizedString("banner_select_validation");
            ViewBag.BannerSave = _localizationService.GetLocalizedString("banner_save");
            ViewBag.BannerUpdate = _localizationService.GetLocalizedString("banner_update");
            ViewBag.BannerSaving = _localizationService.GetLocalizedString("banner_saving");
            ViewBag.BannerUpdating = _localizationService.GetLocalizedString("banner_updating");
            ViewBag.ReviewStatus = _localizationService.GetLocalizedString("review_status");
            ViewBag.ReviewApprove = _localizationService.GetLocalizedString("review_approve");
            ViewBag.ReviewReject = _localizationService.GetLocalizedString("review_reject");

            // Sidebar strings
            ViewBag.SidebarProducts = _localizationService.GetLocalizedString("sidebar_products");
            ViewBag.SidebarOrders = _localizationService.GetLocalizedString("sidebar_orders");

            // Table header strings
            ViewBag.TableHeaderIcon = _localizationService.GetLocalizedString("table_header_icon");
            ViewBag.TableHeaderActions = ViewBag.Actions; // Reuse common Actions

            // Search strings  
            ViewBag.CategorySearch = ViewBag.Search; // Reuse common Search
            ViewBag.OrderSearch = ViewBag.Search; // Reuse common Search
            ViewBag.UserSearch = ViewBag.Search; // Reuse common Search
            ViewBag.ReviewSearch = ViewBag.Search; // Reuse common Search
            ViewBag.BannerSearch = ViewBag.Search; // Reuse common Search

            // Status strings
            ViewBag.CategoryPublished = ViewBag.Published; // Reuse common Published
            ViewBag.CategoryUnpublished = ViewBag.Unpublished; // Reuse common Unpublished
            ViewBag.CategoryProduct = _localizationService.GetLocalizedString("category_product");
            ViewBag.StatusPublished = ViewBag.Published; // Reuse common Published
            ViewBag.StatusUnpublished = ViewBag.Unpublished; // Reuse common Unpublished
            ViewBag.StatusActive = ViewBag.Active; // Reuse common Active
            ViewBag.StatusInactive = ViewBag.Inactive; // Reuse common Inactive

            // Action strings
            ViewBag.ActionEdit = ViewBag.Edit; // Reuse common Edit
            ViewBag.ActionDelete = ViewBag.Delete; // Reuse common Delete
            ViewBag.ActionView = ViewBag.View; // Reuse common View
            ViewBag.SelectAll = _localizationService.GetLocalizedString("select_all");
            ViewBag.CategoryAddNew = _localizationService.GetLocalizedString("category_add_new");

            // auth strings
            ViewBag.AuthSignInTitle = _localizationService.GetLocalizedString("auth_signin_title");
            ViewBag.AuthSignInSubtitle = _localizationService.GetLocalizedString("auth_signin_subtitle");
            ViewBag.AuthEmail = _localizationService.GetLocalizedString("auth_email");
            ViewBag.AuthSignInEmailPlaceholder = _localizationService.GetLocalizedString("auth_signin_email_placeholder");
            ViewBag.AuthEmailAddress = _localizationService.GetLocalizedString("auth_email_address");
            ViewBag.AuthPassword = _localizationService.GetLocalizedString("auth_password");
            ViewBag.AuthRememberMe = _localizationService.GetLocalizedString("auth_remember_me");
            ViewBag.AuthForgotPassword = _localizationService.GetLocalizedString("auth_forgot_password");
            ViewBag.AuthResetPassword = _localizationService.GetLocalizedString("auth_reset_password");
            ViewBag.AuthSignInButton = _localizationService.GetLocalizedString("auth_signin_button");
            ViewBag.AuthNoAccount = _localizationService.GetLocalizedString("auth_no_account");
            ViewBag.AuthSignUpLink = _localizationService.GetLocalizedString("auth_signup_link");
            ViewBag.ValidationEnterEmail = _localizationService.GetLocalizedString("validation_enter_email");
            ViewBag.ValidationEnterPassword = _localizationService.GetLocalizedString("validation_enter_password");
            ViewBag.AuthSignInPasswordLabel = _localizationService.GetLocalizedString("auth_signin_password_label");
            ViewBag.AuthSignInPasswordError = _localizationService.GetLocalizedString("auth_signin_password_error");
            // common validation / messages used in JS alerts
            ViewBag.ValidationFillAll = _localizationService.GetLocalizedString("validation_fill_all");
            ViewBag.SignUpSuccessEnterOtp = _localizationService.GetLocalizedString("signup_success_enter_otp");
            ViewBag.SignUpFailed = _localizationService.GetLocalizedString("signup_failed");
            ViewBag.RegisterFailed = _localizationService.GetLocalizedString("register_failed");
            ViewBag.InternalServerError = _localizationService.GetLocalizedString("internal_server_error");
            ViewBag.EmailExists = _localizationService.GetLocalizedString("email_exists");
            ViewBag.ServerConnectionError = _localizationService.GetLocalizedString("server_connection_error");
            ViewBag.WeakPassword = _localizationService.GetLocalizedString("weak_password");
            ViewBag.VerifyEmailNotFound = _localizationService.GetLocalizedString("verify_email_not_found");
            ViewBag.ResendEnterEmailFirst = _localizationService.GetLocalizedString("resend_enter_email_first");
            ViewBag.ResetEmailNotFound = _localizationService.GetLocalizedString("reset_email_not_found");
            ViewBag.ForgotOtpSent = _localizationService.GetLocalizedString("forgot_otp_sent");
            ViewBag.ResetSuccess = _localizationService.GetLocalizedString("reset_success");
            ViewBag.ResetFailed = _localizationService.GetLocalizedString("reset_failed");
            ViewBag.GenericError = _localizationService.GetLocalizedString("generic_error");
            ViewBag.SignInSuccess = _localizationService.GetLocalizedString("signin_success");
            ViewBag.SignInFailed = _localizationService.GetLocalizedString("signin_failed");
            // verify strings
            ViewBag.VerifySuccess = _localizationService.GetLocalizedString("verify_success");
            ViewBag.VerifyTitle = _localizationService.GetLocalizedString("verify_title");
            ViewBag.VerifySubtitle = _localizationService.GetLocalizedString("verify_subtitle");
            ViewBag.OtpLabel = _localizationService.GetLocalizedString("otp_label");
ViewBag.OtpPlaceholder = _localizationService.GetLocalizedString("otp_placeholder");
ViewBag.VerifyButton = _localizationService.GetLocalizedString("verify_button");
ViewBag.ResendOtpButton = _localizationService.GetLocalizedString("resend_otp_button");
             // sign up strings
            ViewBag.AuthSignUpTitle = _localizationService.GetLocalizedString("auth_signup_title");
            ViewBag.AuthSignUpSubtitle = _localizationService.GetLocalizedString("auth_signup_subtitle");
            ViewBag.AuthFirstName = _localizationService.GetLocalizedString("auth_first_name");
            ViewBag.AuthLastName = _localizationService.GetLocalizedString("auth_last_name");
            ViewBag.AuthEmail = _localizationService.GetLocalizedString("auth_email");
            ViewBag.AuthEmailAddress = _localizationService.GetLocalizedString("auth_email_address");
            ViewBag.AuthPassword = _localizationService.GetLocalizedString("auth_password");
            ViewBag.AuthRegisterButton = _localizationService.GetLocalizedString("auth_register_button");
            ViewBag.AuthHaveAccount = _localizationService.GetLocalizedString("auth_have_account");
            ViewBag.AuthSignInLink = _localizationService.GetLocalizedString("auth_signin_link");
            ViewBag.AuthTermsText = _localizationService.GetLocalizedString("auth_terms_text");
            ViewBag.AuthTermsLink = _localizationService.GetLocalizedString("auth_terms_link");
            ViewBag.AuthPrivacyLink = _localizationService.GetLocalizedString("auth_privacy_link");
            ViewBag.ValidationEnterFirstName = _localizationService.GetLocalizedString("validation_enter_first_name");
            ViewBag.ValidationEnterLastName = _localizationService.GetLocalizedString("validation_enter_last_name");
            ViewBag.ValidationEnterEmail = _localizationService.GetLocalizedString("validation_enter_email");
            ViewBag.ValidationEnterPassword = _localizationService.GetLocalizedString("validation_enter_password");
            ViewBag.AuthForgotPasswordLink = _localizationService.GetLocalizedString("auth_forgot_password_link");
            ViewBag.AuthPhonePlaceholder = _localizationService.GetLocalizedString("auth_phone_placeholder");
            // Forgot Password strings
            ViewBag.AuthForgotPasswordTitle = _localizationService.GetLocalizedString("auth_forgot_password_title");
            ViewBag.AuthForgotPasswordSubtitle = _localizationService.GetLocalizedString("auth_forgot_password_subtitle");
            ViewBag.AuthResetPasswordButton = _localizationService.GetLocalizedString("auth_reset_password_button");
            ViewBag.AuthBackButton = _localizationService.GetLocalizedString("auth_back_button");
            
            // Reports page strings
            ViewBag.ReportsTitle = _localizationService.GetLocalizedString("reportsTitle");
            ViewBag.TotalRevenue = _localizationService.GetLocalizedString("totalRevenue");
            ViewBag.TotalOrders = _localizationService.GetLocalizedString("totalOrders");
            ViewBag.TotalUsers = _localizationService.GetLocalizedString("totalUsers");
            ViewBag.AverageOrderValue = _localizationService.GetLocalizedString("averageOrderValue");
            ViewBag.Last30Days = _localizationService.GetLocalizedString("last30Days");
            ViewBag.SearchByDate = _localizationService.GetLocalizedString("searchByDate");
            ViewBag.DateRange = _localizationService.GetLocalizedString("dateRange");
            ViewBag.FromDate = _localizationService.GetLocalizedString("fromDate");
            ViewBag.ToDate = _localizationService.GetLocalizedString("toDate");
            ViewBag.AllTime = _localizationService.GetLocalizedString("allTime");
            ViewBag.ThisWeek = _localizationService.GetLocalizedString("thisWeek");
            ViewBag.ThisMonth = _localizationService.GetLocalizedString("thisMonth");
            ViewBag.ThisQuarter = _localizationService.GetLocalizedString("thisQuarter");
            ViewBag.ThisYear = _localizationService.GetLocalizedString("thisYear");
            ViewBag.Date = _localizationService.GetLocalizedString("date");
            ViewBag.Revenue = _localizationService.GetLocalizedString("revenue");
            ViewBag.Showing = _localizationService.GetLocalizedString("showing");
            ViewBag.To = _localizationService.GetLocalizedString("to");
            ViewBag.Of = _localizationService.GetLocalizedString("of");
            ViewBag.Entries = _localizationService.GetLocalizedString("entries");
            ViewBag.Previous = _localizationService.GetLocalizedString("previous");
            ViewBag.Next = _localizationService.GetLocalizedString("next");

            // User settings messages
            ViewBag.UpdateProfileSuccess = _localizationService.GetLocalizedString("update_profile_success");
            ViewBag.PasswordChangeSuccess = _localizationService.GetLocalizedString("password_change_success");
            ViewBag.InvalidPassword = _localizationService.GetLocalizedString("invalid_password");

            // Password labels
            ViewBag.NewPasswordLabel = _localizationService.GetLocalizedString("new_password_label");
            ViewBag.CurrentPasswordLabel = _localizationService.GetLocalizedString("current_password_label");
            ViewBag.NewPasswordHelp = _localizationService.GetLocalizedString("new_password_help");
            ViewBag.CurrentPasswordHelp = _localizationService.GetLocalizedString("current_password_help");

            ViewBag.SearchByName = _localizationService.GetLocalizedString("search_by_name");
            ViewBag.TagOptional = _localizationService.GetLocalizedString("tag_optional");
            ViewBag.SortLabel = _localizationService.GetLocalizedString("sort_label");
            ViewBag.ViewDetails = _localizationService.GetLocalizedString("view_details");
            ViewBag.AddedToCartSuccess = _localizationService.GetLocalizedString("added_to_cart_success");
            ViewBag.CartUpdatedSuccess = _localizationService.GetLocalizedString("cart_updated_success");

            ViewBag.AddressTitle = _localizationService.GetLocalizedString("address_title");
            ViewBag.AddressAddNew = _localizationService.GetLocalizedString("address_add_new_button");
            ViewBag.AddressModalTitle = _localizationService.GetLocalizedString("address_modal_title");
            ViewBag.AddressType = _localizationService.GetLocalizedString("address_type");
            ViewBag.AddressTypeHome = _localizationService.GetLocalizedString("address_type_home");
            ViewBag.AddressTypeOffice = _localizationService.GetLocalizedString("address_type_office");
            ViewBag.AddressTypeOther = _localizationService.GetLocalizedString("address_type_other");
            ViewBag.FullNameLabel = _localizationService.GetLocalizedString("full_name_label");
            ViewBag.StreetAddressLabel = _localizationService.GetLocalizedString("street_address_label");
            ViewBag.CityLabel = _localizationService.GetLocalizedString("city_label");
            ViewBag.StateLabel = _localizationService.GetLocalizedString("state_label");
            ViewBag.ZipCodeLabel = _localizationService.GetLocalizedString("zip_code_label");
            ViewBag.PhoneLabel = _localizationService.GetLocalizedString("phone_label");
            ViewBag.SetDefaultAddress = _localizationService.GetLocalizedString("set_default_address");
            ViewBag.DefaultAddressBadge = _localizationService.GetLocalizedString("default_address_badge");
            ViewBag.DeleteAddressConfirm = _localizationService.GetLocalizedString("delete_address_confirm");
            ViewBag.NoAddressesMessage = _localizationService.GetLocalizedString("no_addresses_message");
            ViewBag.LoadAddressesError = _localizationService.GetLocalizedString("load_addresses_error");
            ViewBag.SaveAddressButton = _localizationService.GetLocalizedString("save_address_button");

            ViewBag.PaymentMethodTitle = _localizationService.GetLocalizedString("payment_method_title");
            ViewBag.PaymentTypeLabel = _localizationService.GetLocalizedString("payment_type_label");
            ViewBag.PaymentLabelLabel = _localizationService.GetLocalizedString("payment_label_label");
            ViewBag.PaymentExpiryLabel = _localizationService.GetLocalizedString("payment_expiry_label");
            ViewBag.PaymentAccountLabel = _localizationService.GetLocalizedString("payment_account_label");
            ViewBag.SetDefaultPayment = _localizationService.GetLocalizedString("payment_set_default");
            ViewBag.PaymentDefaultBadge = _localizationService.GetLocalizedString("payment_default_badge");
            ViewBag.NoPaymentMethodsMessage = _localizationService.GetLocalizedString("no_payment_methods_message");
            ViewBag.LoadPaymentError = _localizationService.GetLocalizedString("load_payment_error");

            // Chat widget strings
            ViewBag.ChatTitle = _localizationService.GetLocalizedString("chat_title");
            ViewBag.ChatPlaceholder = _localizationService.GetLocalizedString("chat_placeholder");
            ViewBag.ChatSend = _localizationService.GetLocalizedString("chat_send");
            ViewBag.ChatWelcome = _localizationService.GetLocalizedString("chat_welcome");
            ViewBag.ChatConnectionError = _localizationService.GetLocalizedString("chat_connection_error");

            // Checkout & Order Status & Messages
            ViewBag.CheckoutTitle = _localizationService.GetLocalizedString("checkout_title");
            ViewBag.CheckoutAddress = _localizationService.GetLocalizedString("checkout_address");
            ViewBag.CheckoutSelectAddress = _localizationService.GetLocalizedString("checkout_select_address");
            ViewBag.CheckoutShippingFee = _localizationService.GetLocalizedString("checkout_shipping_fee");
            ViewBag.CheckoutSubtotal = _localizationService.GetLocalizedString("checkout_subtotal");
            ViewBag.CheckoutDiscount = _localizationService.GetLocalizedString("checkout_discount");
            ViewBag.CheckoutTotal = _localizationService.GetLocalizedString("checkout_total");
            ViewBag.CheckoutPlaceOrder = _localizationService.GetLocalizedString("checkout_place_order");
            ViewBag.CheckoutPaymentMethod = _localizationService.GetLocalizedString("checkout_payment_method");
            ViewBag.CheckoutCouponCode = _localizationService.GetLocalizedString("checkout_coupon_code");

            ViewBag.StatusPlaced = _localizationService.GetLocalizedString("status_placed");
            ViewBag.StatusProcessing = _localizationService.GetLocalizedString("status_processing");
            ViewBag.StatusShipping = _localizationService.GetLocalizedString("status_shipping");
            ViewBag.StatusPaid = _localizationService.GetLocalizedString("status_paid");
            ViewBag.StatusCompleted = _localizationService.GetLocalizedString("status_completed");
            ViewBag.StatusCancelled = _localizationService.GetLocalizedString("status_cancelled");

            ViewBag.MsgSelectAddress = _localizationService.GetLocalizedString("msg_select_address");
            ViewBag.MsgCartEmpty = _localizationService.GetLocalizedString("msg_cart_empty");
            ViewBag.MsgOutOfStock = _localizationService.GetLocalizedString("msg_out_of_stock");
            ViewBag.MsgPaymentError = _localizationService.GetLocalizedString("msg_payment_error");
            ViewBag.MsgCheckoutFailed = _localizationService.GetLocalizedString("msg_checkout_failed");
            ViewBag.MsgServerError = _localizationService.GetLocalizedString("msg_server_error");
            ViewBag.MsgLoginRequired = _localizationService.GetLocalizedString("msg_login_required");
            ViewBag.LoadMore = _localizationService.GetLocalizedString("load_more");

            // Coupon strings
            ViewBag.CouponTitle = _localizationService.GetLocalizedString("couponTitle");
            ViewBag.CouponAddNew = _localizationService.GetLocalizedString("couponAddNew");
            ViewBag.CouponEdit = _localizationService.GetLocalizedString("couponEdit");
            ViewBag.CouponBackToList = _localizationService.GetLocalizedString("couponBackToList");
            ViewBag.CouponSearch = _localizationService.GetLocalizedString("couponSearch");
            ViewBag.CouponCode = _localizationService.GetLocalizedString("couponCode");
            ViewBag.CouponName = _localizationService.GetLocalizedString("couponName");
            ViewBag.CouponType = _localizationService.GetLocalizedString("couponType");
            ViewBag.CouponValue = _localizationService.GetLocalizedString("couponValue");
            ViewBag.CouponPeriod = _localizationService.GetLocalizedString("couponPeriod");
            ViewBag.CouponUsage = _localizationService.GetLocalizedString("couponUsage");
            ViewBag.CouponStatus = _localizationService.GetLocalizedString("couponStatus");
            ViewBag.CouponStart = _localizationService.GetLocalizedString("couponStart");
            ViewBag.CouponEnd = _localizationService.GetLocalizedString("couponEnd");
            ViewBag.CouponRemaining = _localizationService.GetLocalizedString("couponRemaining");
            ViewBag.CouponActive = _localizationService.GetLocalizedString("couponActive");
            ViewBag.CouponInactive = _localizationService.GetLocalizedString("couponInactive");
            ViewBag.CouponPending = _localizationService.GetLocalizedString("couponPending");
            ViewBag.CouponExpired = _localizationService.GetLocalizedString("couponExpired");
            ViewBag.CouponActivate = _localizationService.GetLocalizedString("couponActivate");
            ViewBag.CouponDeactivate = _localizationService.GetLocalizedString("couponDeactivate");
            ViewBag.CouponDelete = _localizationService.GetLocalizedString("couponDelete");
            ViewBag.CouponDeleteConfirm = _localizationService.GetLocalizedString("couponDeleteConfirm");
            ViewBag.CouponNoData = _localizationService.GetLocalizedString("couponNoData");
            ViewBag.CouponBasicInfo = _localizationService.GetLocalizedString("couponBasicInfo");
            ViewBag.CouponSettings = _localizationService.GetLocalizedString("couponSettings");
            ViewBag.CouponPreview = _localizationService.GetLocalizedString("couponPreview");
            ViewBag.CouponCodePlaceholder = _localizationService.GetLocalizedString("couponCodePlaceholder");
            ViewBag.CouponNamePlaceholder = _localizationService.GetLocalizedString("couponNamePlaceholder");
            ViewBag.CouponSelectType = _localizationService.GetLocalizedString("couponSelectType");
            ViewBag.CouponPercentage = _localizationService.GetLocalizedString("couponPercentage");
            ViewBag.CouponFixedAmount = _localizationService.GetLocalizedString("couponFixedAmount");
            ViewBag.CouponValuePlaceholder = _localizationService.GetLocalizedString("couponValuePlaceholder");
            ViewBag.CouponValueUnit = _localizationService.GetLocalizedString("couponValueUnit");
            ViewBag.CouponStartDate = _localizationService.GetLocalizedString("couponStartDate");
            ViewBag.CouponEndDate = _localizationService.GetLocalizedString("couponEndDate");
            ViewBag.CouponTotalQuantity = _localizationService.GetLocalizedString("couponTotalQuantity");
            ViewBag.CouponTotalPlaceholder = _localizationService.GetLocalizedString("couponTotalPlaceholder");
            ViewBag.CouponProductOptional = _localizationService.GetLocalizedString("couponProductOptional");
            ViewBag.CouponAllProducts = _localizationService.GetLocalizedString("couponAllProducts");
            ViewBag.CouponProductNote = _localizationService.GetLocalizedString("couponProductNote");
            ViewBag.CouponDescription = _localizationService.GetLocalizedString("couponDescription");
            ViewBag.CouponDescriptionPlaceholder = _localizationService.GetLocalizedString("couponDescriptionPlaceholder");
            ViewBag.CouponSave = _localizationService.GetLocalizedString("couponSave");
            ViewBag.CouponUpdate = _localizationService.GetLocalizedString("couponUpdate");
            ViewBag.CouponCancel = _localizationService.GetLocalizedString("couponCancel");
            ViewBag.CouponCodePreview = _localizationService.GetLocalizedString("couponCodePreview");
            ViewBag.CouponNamePreview = _localizationService.GetLocalizedString("couponNamePreview");
            ViewBag.CouponValuePreview = _localizationService.GetLocalizedString("couponValuePreview");
            ViewBag.CouponTypePreview = _localizationService.GetLocalizedString("couponTypePreview");
            ViewBag.CouponPeriodPreview = _localizationService.GetLocalizedString("couponPeriodPreview");
            ViewBag.CouponUsageStats = _localizationService.GetLocalizedString("couponUsageStats");
            ViewBag.CouponUsed = _localizationService.GetLocalizedString("couponUsed");
            ViewBag.CouponCreatedAt = _localizationService.GetLocalizedString("couponCreatedAt");
            ViewBag.CouponUpdatedAt = _localizationService.GetLocalizedString("couponUpdatedAt");
            ViewBag.CouponCurrentStatus = _localizationService.GetLocalizedString("couponCurrentStatus");
            ViewBag.CouponDateError = _localizationService.GetLocalizedString("couponDateError");
            ViewBag.CouponPercentageError = _localizationService.GetLocalizedString("couponPercentageError");
            ViewBag.CouponAmountError = _localizationService.GetLocalizedString("couponAmountError");
            ViewBag.CouponTotalError = _localizationService.GetLocalizedString("couponTotalError");
        }

        protected string GetLocalizedString(string key)
        {
            return _localizationService.GetLocalizedString(key);
        }
    }
}
