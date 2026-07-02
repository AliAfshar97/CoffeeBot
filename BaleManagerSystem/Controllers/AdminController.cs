using BaleManagerSystem.Models;
using BaleManagerSystem.Models.ViewModels;
using BaleManagerSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;

namespace BaleManagerSystem.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly BaleMessageService _baleService;

        private readonly SafirUserRepository _repo;

        private readonly BroadcastService _broadcast;

        private readonly IUserRepository _userRepository;

        private readonly IOrderRepository _orderRepository;

        private readonly ICoffeePriceRepository _priceRepository;

        private readonly IMenuRepository _menuRepository;

        private readonly PaymentReportExcelExporter _excelExporter;

        private readonly IAccountRepository _accountRepository;

        private readonly AccountBalancesExcelExporter _accountExcelExporter;

        private readonly ReceiptFileService _receiptFiles;

        private readonly ITelegramBotClient _botClient;

        private readonly AccountStatementService _accountStatementService;

        public AdminController(
            BaleMessageService baleService,
            SafirUserRepository repo,
            BroadcastService broadcast,
            IUserRepository userRepository,
            IOrderRepository orderRepository,
            ICoffeePriceRepository priceRepository,
            IMenuRepository menuRepository,
            PaymentReportExcelExporter excelExporter,
            IAccountRepository accountRepository,
            AccountBalancesExcelExporter accountExcelExporter,
            ReceiptFileService receiptFiles,
            ITelegramBotClient botClient,
            AccountStatementService accountStatementService)
        {
            _baleService = baleService;
            _repo = repo;
            _broadcast = broadcast;
            _userRepository = userRepository;
            _orderRepository = orderRepository;
            _priceRepository = priceRepository;
            _menuRepository = menuRepository;
            _excelExporter = excelExporter;
            _accountRepository = accountRepository;
            _accountExcelExporter = accountExcelExporter;
            _receiptFiles = receiptFiles;
            _botClient = botClient;
            _accountStatementService = accountStatementService;
        }
        // DASHBOARD
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalUsers =
                await _userRepository.GetUserCountAsync();

            ViewBag.TotalOrders =
                await _orderRepository.GetOrderCountAsync();

            return View();
        }

        // ================= REGISTER =================

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            var users =
                await _repo.GetUsersAsync();

            ViewBag.Users = users;

            return View(new RegisterUserViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterUserViewModel model)
        {
            var users =
                    await _repo.GetUsersAsync();

            if (!ModelState.IsValid)
            {
                ViewBag.Users = users;

                return View(model);
            }

            if (users != null)
            {
                var isExistPhoneNumber = users.Where(u => u.PhoneNumber == model.PhoneNumber).Count() > 0;

                if (isExistPhoneNumber)
                {
                    ViewBag.Message =
                       "شماره تلفن همراه کاربر از قبل وارد شده است.";

                    return View(users);
                }
            }
            try
            {
                await _repo.SaveUserAsync(model.PhoneNumber, model.Username);

                ViewBag.Message =
                    "شماره همراه با موفقیت اضافه شد.";
            }
            catch (Exception)
            {
                ViewBag.Message =
                    "ثبت کاربر با خطا مواجه شد.";
            }
            
            var allUsers =
                await _repo.GetUsersAsync();

            ViewBag.Users = allUsers;

            ModelState.Clear();

            return View(new RegisterUserViewModel());
        }

        // ================= SEND =================

        [HttpGet]
        public async Task<IActionResult> Send()
        {
            var vm =
             new BroadcastPageViewModel
             {
                 PhoneUsers =
                     await _repo.GetUsersAsync(),

                 TelegramUsers =
                     await _userRepository.GetAllChatIds()
             };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Send(
            BroadcastPageViewModel vm)
        {
            vm.PhoneUsers =
                await _repo.GetUsersAsync();

            vm.TelegramUsers =
                await _userRepository.GetAllChatIds();

            int success = 0;
            int failed = 0;

            if (vm.RecipientType == "Phone")
            {
                if (vm.SelectedPhones == null ||
                    !vm.SelectedPhones.Any())
                {
                    ViewBag.Error =
                        "حداقل یک شماره تلفن انتخاب کنید.";

                    return View(vm);
                }

                foreach (var phone in vm.SelectedPhones)
                {
                    try
                    {
                        string? fileId = null;

                        if (vm.Attachment != null)
                        {
                            fileId =
                                await _baleService
                                    .UploadFileAsync(
                                        vm.Attachment);
                        }

                        var result =
                            await _baleService.SendMessageAsync(
                                phone,
                                vm.Message,
                                fileId);

                        if (result)
                        {
                            success++;

                            await _repo.SaveLogAsync(
                            vm.Message,
                            true,
                            null,
                            null,
                            phone);
                        }
                        else
                        {
                            failed++;

                            await _repo.SaveLogAsync(
                                
                                vm.Message,
                                false,
                                "Send failed",
                                null,
                                phone);
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;

                        await _repo.SaveLogAsync(
                            vm.Message,
                            false,
                            ex.Message,
                            null,
                            phone);
                    }
                }
            }
            else
            {
                if (vm.SelectedChatIds == null ||
                    !vm.SelectedChatIds.Any())
                {
                    ViewBag.Error =
                        "حداقل یک شناسه چت انتخاب کنید.";

                    return View(vm);
                }

                if (vm.IncludeAccountStatement)
                {
                    if (!vm.StatementFromDate.HasValue ||
                        !vm.StatementToDate.HasValue ||
                        !vm.PaymentDueDate.HasValue)
                    {
                        ViewBag.Error =
                            "برای پیوست صورت‌حساب، تاریخ شروع، پایان و مهلت پرداخت را وارد کنید.";

                        return View(vm);
                    }

                    if (vm.StatementFromDate > vm.StatementToDate)
                    {
                        ViewBag.Error =
                            "تاریخ شروع نمی‌تواند بعد از تاریخ پایان باشد.";

                        return View(vm);
                    }
                }

                string? fileId = null;

                if (vm.Attachment != null)
                {
                    fileId = await _baleService.UploadFileAsync(vm.Attachment);
                }

                foreach (var chatId in vm.SelectedChatIds.Distinct())
                {
                    try
                    {
                        var message = vm.Message ?? string.Empty;

                        if (vm.IncludeAccountStatement)
                        {
                            var statement = await _accountStatementService.BuildStatementAsync(
                                chatId,
                                vm.StatementFromDate!.Value,
                                vm.StatementToDate!.Value,
                                vm.PaymentDueDate!.Value);

                            message = string.IsNullOrWhiteSpace(message)
                                ? statement.TrimStart()
                                : message + statement;
                        }

                        if (string.IsNullOrWhiteSpace(message))
                        {
                            failed++;
                            continue;
                        }

                        var result = await _broadcast.SendToUsers(
                            new List<long> { chatId },
                            message,
                            fileId);

                        if (result.SuccessCount > 0)
                        {
                            success++;

                            await _repo.SaveLogAsync(
                                message,
                                true,
                                null,
                                chatId,
                                null);
                        }
                        else
                        {
                            failed++;

                            await _repo.SaveLogAsync(
                                message,
                                false,
                                "Send failed",
                                chatId,
                                null);
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;

                        await _repo.SaveLogAsync(
                            vm.Message,
                            false,
                            ex.Message,
                            chatId,
                            null);
                    }
                }
            }

            ViewBag.Success = success;

            ViewBag.Failed = failed;

            return View(vm);
        }

        // ================= LOGS =================

        public async Task<IActionResult> Logs()
        {
            var getLog = await _repo.GetLogsAsync();

            return View(getLog);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            await _repo.DeleteUserAsync(id);

            return RedirectToAction(nameof(Register));
        }


        [HttpPost("selected")]
        public async Task<IActionResult> SendSelected(
            [FromBody] BroadcastRequest request)
        {
            await _broadcast.SendToUsers(
                request.ChatIds,
                request.Message);

            return Ok();
        }


        public async Task<IActionResult> Orders()
        {
            var data = await _orderRepository.GetOrdersAsync();

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> CreateOrder()
        {
            var vm = new CreateOrderViewModel
            {
                MenuItems = await _menuRepository.GetActiveOrderedAsync(),
                Users = await _userRepository.GetAllChatIds(),
                ShotCount = 1
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(CreateOrderViewModel model)
        {
            if (model.ChatId == 0)
            {
                TempData["Error"] = "لطفاً کاربر را انتخاب کنید.";
                return await ReloadCreateOrderView(model);
            }

            if (string.IsNullOrWhiteSpace(model.DrinkType) || model.ShotCount < 1)
            {
                TempData["Error"] = "نوشیدنی و تعداد معتبر انتخاب کنید.";
                return await ReloadCreateOrderView(model);
            }

            var item = await _menuRepository.GetByKeyAsync(model.DrinkType);

            if (item == null)
            {
                TempData["Error"] = "نوشیدنی انتخاب‌شده معتبر نیست.";
                return await ReloadCreateOrderView(model);
            }

            var price = model.PriceInToman;

            if (price <= 0)
            {
                price = await _priceRepository.GetPriceAsync(
                    model.DrinkType,
                    model.ShotCount,
                    withChocolate: false) ?? 0;
            }

            if (price < 0)
            {
                TempData["Error"] = "مبلغ نامعتبر است.";
                return await ReloadCreateOrderView(model);
            }

            var displayName = model.DisplayName;

            if (string.IsNullOrWhiteSpace(displayName))
            {
                var user = await _userRepository.GetUserByChatIdAsync(model.ChatId);
                displayName = user?.DisplayName ?? "نامشخص";
            }

            var order = new CoffeeOrder
            {
                ChatId = model.ChatId,
                DisplayName = displayName,
                DrinkType = model.DrinkType,
                ShotCount = model.ShotCount,
                WithChocolate = false,
                PriceInToman = price
            };

            var orderId = await _orderRepository.SaveOrderAsync(order);

            await _accountRepository.AddDebitAsync(
                model.ChatId,
                price,
                BuildNewOrderLedgerDescription(item, model.DrinkType, model.ShotCount),
                orderId);

            TempData["Message"] = "سفارش با موفقیت ثبت شد.";

            return RedirectToAction(nameof(Orders));
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderPrice(string drinkType, byte shotCount)
        {
            var price = await _priceRepository.GetPriceAsync(drinkType, shotCount, withChocolate: false);

            return Json(new { price });
        }

        private async Task<IActionResult> ReloadCreateOrderView(CreateOrderViewModel model)
        {
            model.MenuItems = await _menuRepository.GetActiveOrderedAsync();
            model.Users = await _userRepository.GetAllChatIds();

            return View("CreateOrder", model);
        }

        [HttpGet]
        public async Task<IActionResult> EditOrder(int id)
        {
            var order = await _orderRepository.GetOrderByIdAsync(id);

            if (order == null)
            {
                TempData["Error"] = "سفارش یافت نشد.";
                return RedirectToAction(nameof(Orders));
            }

            var vm = new EditOrderViewModel
            {
                Id = order.Id,
                ChatId = order.ChatId,
                DisplayName = order.DisplayName,
                DrinkType = order.DrinkType,
                ShotCount = order.ShotCount,
                PriceInToman = order.PriceInToman,
                CreatedAt = order.CreatedAt,
                MenuItems = await _menuRepository.GetAllAsync()
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> EditOrder(EditOrderViewModel model)
        {
            var order = await _orderRepository.GetOrderByIdAsync(model.Id);

            if (order == null)
            {
                TempData["Error"] = "سفارش یافت نشد.";
                return RedirectToAction(nameof(Orders));
            }

            if (model.PriceInToman < 0 || model.ShotCount < 1)
            {
                TempData["Error"] = "مقدار شات یا مبلغ نامعتبر است.";
                return RedirectToAction(nameof(EditOrder), new { id = model.Id });
            }

            var item = await _menuRepository.GetByKeyAsync(model.DrinkType);

            order.DrinkType = model.DrinkType;
            order.ShotCount = model.ShotCount;
            order.PriceInToman = model.PriceInToman;

            await _orderRepository.UpdateOrderAsync(order);

            // Keep the matching ledger debit in sync so balances stay correct.
            await _accountRepository.UpdateOrderDebitAsync(
                order.Id,
                order.PriceInToman,
                BuildOrderLedgerDescription(item, model.DrinkType, model.ShotCount));

            TempData["Message"] = "سفارش با موفقیت اصلاح شد.";

            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _orderRepository.GetOrderByIdAsync(id);

            if (order == null)
            {
                TempData["Error"] = "سفارش یافت نشد.";
                return RedirectToAction(nameof(Orders));
            }

            // Remove the linked debit first so the user's balance is corrected.
            await _accountRepository.DeleteOrderDebitAsync(id);
            await _orderRepository.DeleteOrderAsync(id);

            TempData["Message"] = "سفارش حذف شد و بدهکاری مربوطه نیز برداشته شد.";

            return RedirectToAction(nameof(Orders));
        }

        private static string BuildOrderLedgerDescription(MenuItem? item, string drinkType, byte shotCount)
        {
            var name = item?.NamePersian ?? drinkType;

            if (item != null && item.SupportsShots)
                return $"سفارش: {name} {shotCount} {item.Unit} (اصلاح‌شده)";

            return $"سفارش: {name} (اصلاح‌شده)";
        }

        private static string BuildNewOrderLedgerDescription(MenuItem? item, string drinkType, byte shotCount)
        {
            var name = item?.NamePersian ?? drinkType;

            if (item != null && item.SupportsShots)
                return $"سفارش: {name} {shotCount} {item.Unit} (دستی)";

            return $"سفارش: {name} (دستی)";
        }

        // ================= MENU MANAGEMENT =================

        private static readonly HashSet<string> ReservedItemKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "menu_order", "menu_receipt", "shots_1", "shots_2", "choc_yes", "choc_no",
            "order_more", "order_done"
        };

        [HttpGet]
        public async Task<IActionResult> Menu(int? editId)
        {
            var vm = new MenuPageViewModel
            {
                Items = await _menuRepository.GetAllAsync()
            };

            if (editId.HasValue)
            {
                var item = await _menuRepository.GetByIdAsync(editId.Value);

                if (item != null)
                {
                    vm.IsEditing = true;
                    vm.Form = await BuildFormFromItemAsync(item);
                }
            }
            else
            {
                vm.Form = new MenuItemFormViewModel
                {
                    Unit = "شات",
                    IsActive = true,
                    DisplayOrder = vm.Items.Count + 1
                };
            }

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> SaveMenuItem(MenuItemFormViewModel form)
        {
            form.ItemKey = (form.ItemKey ?? string.Empty).Trim();
            form.NamePersian = (form.NamePersian ?? string.Empty).Trim();
            form.Unit = string.IsNullOrWhiteSpace(form.Unit) ? "شات" : form.Unit.Trim();

            if (ReservedItemKeys.Contains(form.ItemKey))
            {
                ModelState.AddModelError(nameof(form.ItemKey),
                    "این شناسه رزرو شده است و قابل استفاده نیست.");
            }

            if (ModelState.IsValid &&
                await _menuRepository.KeyExistsAsync(form.ItemKey, form.Id == 0 ? null : form.Id))
            {
                ModelState.AddModelError(nameof(form.ItemKey),
                    "آیتمی با این شناسه از قبل وجود دارد.");
            }

            if (!ModelState.IsValid)
            {
                var vm = new MenuPageViewModel
                {
                    Items = await _menuRepository.GetAllAsync(),
                    Form = form,
                    IsEditing = form.Id != 0
                };

                return View(nameof(Menu), vm);
            }

            var item = new MenuItem
            {
                Id = form.Id,
                ItemKey = form.ItemKey,
                NamePersian = form.NamePersian,
                SupportsShots = form.SupportsShots,
                Unit = form.Unit,
                DisplayOrder = form.DisplayOrder,
                Visibility = form.Visibility,
                IsActive = form.IsActive
            };

            if (form.Id == 0)
            {
                await _menuRepository.CreateAsync(item);
            }
            else
            {
                await _menuRepository.UpdateAsync(item);
            }

            await _priceRepository.SyncItemPricesAsync(item.ItemKey, item.SupportsShots);

            // Persist the prices for the valid quantities only.
            await _priceRepository.UpsertPriceAsync(item.ItemKey, 1, false, form.Price_1);

            if (item.SupportsShots)
            {
                await _priceRepository.UpsertPriceAsync(item.ItemKey, 2, false, form.Price_2);
            }

            TempData["Message"] = form.Id == 0
                ? "آیتم منو با موفقیت اضافه شد."
                : "آیتم منو با موفقیت به‌روزرسانی شد.";

            return RedirectToAction(nameof(Menu));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleMenuItem(int id)
        {
            var item = await _menuRepository.GetByIdAsync(id);

            if (item == null)
            {
                TempData["Error"] = "آیتم یافت نشد.";
                return RedirectToAction(nameof(Menu));
            }

            await _menuRepository.SetActiveAsync(id, !item.IsActive);

            TempData["Message"] = item.IsActive
                ? "آیتم غیرفعال شد و دیگر در ربات نمایش داده نمی‌شود."
                : "آیتم فعال شد.";

            return RedirectToAction(nameof(Menu));
        }

        // ================= BOT USERS (SUBSCRIPTION) =================

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = await _userRepository.GetAllUsersAsync();

            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> SetSubscription(long chatId, bool isSubscriber)
        {
            await _userRepository.SetSubscriptionAsync(chatId, isSubscriber);

            TempData["Message"] = isSubscriber
                ? "کاربر به‌عنوان مشترک ثبت شد."
                : "اشتراک کاربر برداشته شد.";

            return RedirectToAction(nameof(Users));
        }

        private async Task<MenuItemFormViewModel> BuildFormFromItemAsync(MenuItem item)
        {
            var prices = await _priceRepository.GetByDrinkAsync(item.ItemKey);

            int PriceOf(byte shot, bool choc) =>
                prices.FirstOrDefault(p => p.ShotCount == shot && p.WithChocolate == choc)?.PriceInToman ?? 0;

            return new MenuItemFormViewModel
            {
                Id = item.Id,
                ItemKey = item.ItemKey,
                NamePersian = item.NamePersian,
                SupportsShots = item.SupportsShots,
                Unit = item.Unit,
                DisplayOrder = item.DisplayOrder,
                Visibility = item.Visibility,
                IsActive = item.IsActive,
                Price_1 = PriceOf(1, false),
                Price_2 = PriceOf(2, false)
            };
        }

        [HttpGet]
        public async Task<IActionResult> PaymentReport(DateTime? fromDate, DateTime? toDate)
        {
            var report = await _orderRepository.GetPaymentReportAsync(fromDate, toDate);

            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> ExportPaymentReport(DateTime? fromDate, DateTime? toDate)
        {
            var report = await _orderRepository.GetPaymentReportAsync(fromDate, toDate);
            var fileBytes = _excelExporter.Export(report);

            var fileName = BuildReportFileName(fromDate, toDate);

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        private static string BuildReportFileName(DateTime? fromDate, DateTime? toDate)
        {
            if (fromDate.HasValue && toDate.HasValue)
            {
                return $"PaymentReport_{fromDate:yyyy-MM-dd}_to_{toDate:yyyy-MM-dd}.xlsx";
            }

            if (fromDate.HasValue)
            {
                return $"PaymentReport_from_{fromDate:yyyy-MM-dd}.xlsx";
            }

            if (toDate.HasValue)
            {
                return $"PaymentReport_to_{toDate:yyyy-MM-dd}.xlsx";
            }

            return $"PaymentReport_{DateTime.Now:yyyy-MM-dd}.xlsx";
        }

        [HttpGet]
        public async Task<IActionResult> Accounts(DateTime? fromDate, DateTime? toDate)
        {
            var report = await _accountRepository.GetAccountsAsync(fromDate, toDate);
            ViewBag.Users = await _userRepository.GetAllChatIds();
            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> ExportAccounts(DateTime? fromDate, DateTime? toDate)
        {
            var report = await _accountRepository.GetAccountsAsync(fromDate, toDate);
            var fileBytes = _accountExcelExporter.Export(report);
            var fileName = BuildAccountFileName(fromDate, toDate);

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        [HttpPost]
        public async Task<IActionResult> AddCredit(AddCreditViewModel model)
        {
            if (model.Amount <= 0)
            {
                TempData["Error"] = "مبلغ بستانکاری باید بیشتر از صفر باشد.";
                return RedirectToAction(nameof(Accounts));
            }

            await _accountRepository.AddCreditAsync(
                model.ChatId,
                model.Amount,
                model.Description ?? "شارژ دستی توسط مدیر",
                null,
                User.Identity?.Name ?? "admin");

            TempData["Message"] = $"مبلغ {model.Amount:N0} تومان برای {model.DisplayName} به حساب اضافه شد.";

            return RedirectToAction(nameof(Accounts));
        }

        [HttpPost]
        public async Task<IActionResult> AddDebit(AddDebitViewModel model)
        {
            if (model.Amount <= 0)
            {
                TempData["Error"] = "مبلغ بدهکاری باید بیشتر از صفر باشد.";
                return RedirectToAction(nameof(Accounts));
            }

            await _accountRepository.AddManualDebitAsync(
                model.ChatId,
                model.Amount,
                model.Description ?? "بدهکاری دستی توسط مدیر",
                User.Identity?.Name ?? "admin");

            TempData["Message"] = $"مبلغ {model.Amount:N0} تومان بدهکاری برای {model.DisplayName} ثبت شد.";

            return RedirectToAction(nameof(Accounts));
        }

        [HttpGet]
        public async Task<IActionResult> Receipts()
        {
            var pending = await _accountRepository.GetReceiptsAsync(ReceiptStatuses.Pending);
            var processed = await _accountRepository.GetReceiptsAsync(null);

            ViewBag.ProcessedReceipts = processed
                .Where(r => r.Status != ReceiptStatuses.Pending)
                .ToList();

            return View(pending);
        }

        [HttpGet]
        public async Task<IActionResult> ReceiptImage(int id, CancellationToken cancellationToken)
        {
            var receipt = await _accountRepository.GetReceiptByIdAsync(id);

            if (receipt == null)
                return NotFound();

            var (imageBytes, contentType) = await _receiptFiles.GetReceiptImageAsync(
                _botClient,
                receipt,
                cancellationToken);

            if (imageBytes == null || imageBytes.Length == 0)
                return NotFound();

            return File(imageBytes, contentType);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveReceipt(ApproveReceiptViewModel model)
        {
            if (model.CreditAmount <= 0)
            {
                TempData["Error"] = "مبلغ بستانکاری باید بیشتر از صفر باشد.";
                return RedirectToAction(nameof(Receipts));
            }

            var receipt = await _accountRepository.GetReceiptByIdAsync(model.ReceiptId);

            if (receipt == null || receipt.Status != ReceiptStatuses.Pending)
            {
                TempData["Error"] = "رسید یافت نشد یا قبلاً بررسی شده است.";
                return RedirectToAction(nameof(Receipts));
            }

            try
            {
                await _accountRepository.ApproveReceiptAsync(
                    model.ReceiptId,
                    model.CreditAmount,
                    model.AdminNote,
                    User.Identity?.Name ?? "admin");
            }
            catch (InvalidOperationException)
            {
                TempData["Error"] = "رسید یافت نشد یا قبلاً بررسی شده است.";
                return RedirectToAction(nameof(Receipts));
            }

            await TryNotifyClientAboutReceiptAsync(
                receipt,
                approved: true,
                model.CreditAmount,
                model.AdminNote);

            TempData["Message"] = "رسید تایید شد و بستانکاری ثبت گردید.";

            return RedirectToAction(nameof(Receipts));
        }

        [HttpPost]
        public async Task<IActionResult> RejectReceipt(int receiptId, string? adminNote)
        {
            var receipt = await _accountRepository.GetReceiptByIdAsync(receiptId);

            if (receipt == null || receipt.Status != ReceiptStatuses.Pending)
            {
                TempData["Error"] = "رسید یافت نشد یا قبلاً بررسی شده است.";
                return RedirectToAction(nameof(Receipts));
            }

            await _accountRepository.RejectReceiptAsync(receiptId, adminNote);

            await TryNotifyClientAboutReceiptAsync(
                receipt,
                approved: false,
                creditAmount: null,
                adminNote);

            TempData["Message"] = "رسید رد شد.";

            return RedirectToAction(nameof(Receipts));
        }

        private async Task TryNotifyClientAboutReceiptAsync(
            PaymentReceipt receipt,
            bool approved,
            int? creditAmount,
            string? adminNote)
        {
            try
            {
                var message = approved
                    ? BuildApprovedReceiptMessage(receipt, creditAmount!.Value, adminNote)
                    : BuildRejectedReceiptMessage(receipt, adminNote);

                await _botClient.SendMessage(receipt.ChatId, message);
            }
            catch
            {
                TempData["Error"] = (TempData["Error"] as string) ??
                    "عملیات انجام شد، اما ارسال پیام به کاربر در ربات ناموفق بود.";
            }
        }

        private static string BuildApprovedReceiptMessage(
            PaymentReceipt receipt,
            int creditAmount,
            string? adminNote)
        {
            var message =
                "رسید پرداخت شما توسط مدیر تایید شد.\n\n" +
                $"شماره رسید: {receipt.Id}\n" +
                $"مبلغ شارژ: {creditAmount:N0} تومان";

            if (!string.IsNullOrWhiteSpace(adminNote))
            {
                message += $"\nیادداشت مدیر: {adminNote}";
            }

            message += "\n\nحساب شما به‌روزرسانی شد.\n" +
                       "برای مشاهده منو /start را ارسال کنید.";

            return message;
        }

        private static string BuildRejectedReceiptMessage(
            PaymentReceipt receipt,
            string? adminNote)
        {
            var message =
                "رسید پرداخت شما توسط مدیر رد شد.\n\n" +
                $"شماره رسید: {receipt.Id}";

            if (!string.IsNullOrWhiteSpace(adminNote))
            {
                message += $"\nدلیل: {adminNote}";
            }

            message += "\n\nدر صورت نیاز می‌توانید رسید جدید از منوی ربات ارسال کنید.\n" +
                       "برای باز کردن منو /start را ارسال کنید.";

            return message;
        }

        private static string BuildAccountFileName(DateTime? fromDate, DateTime? toDate)
        {
            if (fromDate.HasValue && toDate.HasValue)
            {
                return $"AccountBalances_{fromDate:yyyy-MM-dd}_to_{toDate:yyyy-MM-dd}.xlsx";
            }

            if (fromDate.HasValue)
            {
                return $"AccountBalances_from_{fromDate:yyyy-MM-dd}.xlsx";
            }

            if (toDate.HasValue)
            {
                return $"AccountBalances_to_{toDate:yyyy-MM-dd}.xlsx";
            }

            return $"AccountBalances_{DateTime.Now:yyyy-MM-dd}.xlsx";
        }
    }
}