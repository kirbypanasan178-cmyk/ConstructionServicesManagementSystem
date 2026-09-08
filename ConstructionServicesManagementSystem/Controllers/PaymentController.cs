using ConstructionServicesManagementSystem.Enums;
using ConstructionServicesManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionServicesManagementSystem.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // GET: /Payment
        public async Task<IActionResult> Index(int page = 1, string? search = null)
        {
            int pageSize = 10;

            var billings = await _paymentService.GetPendingBillingsAsync(page, pageSize, search);

            ViewBag.CurrentPage = page;
            ViewBag.Search = search;
            ViewBag.PageSize = pageSize;
            ViewBag.HasNextPage = billings.Count == pageSize;
            ViewBag.HasPreviousPage = page > 1;

            return View(billings);
        }

        // GET: /Payment/Process/5
        public async Task<IActionResult> Process(int id)
        {
            var billing = await _paymentService.GetBillingByIdAsync(id);

            if (billing == null)
            {
                return NotFound();
            }

            if (billing.Balance <= 0)
            {
                TempData["Error"] = "This billing has already been fully paid.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.PaymentMethods = Enum.GetValues<PaymentMethod>();

            return View(billing);
        }

        // POST: /Payment/Process
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(
            int billingId,
            decimal amount,
            PaymentMethod paymentMethod)
        {
            var billing = await _paymentService.GetBillingByIdAsync(billingId);

            if (billing == null)
            {
                return NotFound();
            }

            if (amount <= 0)
            {
                ModelState.AddModelError(
                    "amount",
                    "Payment amount must be greater than zero.");
            }

            if (amount > billing.Balance)
            {
                ModelState.AddModelError(
                    "amount",
                    $"Payment cannot exceed the remaining balance of ₱{billing.Balance:N2}.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.PaymentMethods = Enum.GetValues<PaymentMethod>();

                return View(billing);
            }

            var payment = await _paymentService.ProcessPaymentAsync(
                billingId,
                amount,
                paymentMethod);

            if (payment == null)
            {
                TempData["Error"] = "Unable to process the payment.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                $"Payment processed successfully. Reference Number: {payment.ReferenceNumber}";

            return RedirectToAction(nameof(Index));
        }
    }
}