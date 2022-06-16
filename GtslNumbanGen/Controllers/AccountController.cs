using AspNetCoreHero.ToastNotification.Abstractions;

using CsvHelper;
using CsvHelper.Configuration;

using GtslNumbanGen.Models;

using Microsoft.AspNetCore.Mvc;

using System.Globalization;

namespace GtslNumbanGen.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly INotyfService _notyf;

        public AccountController(ILogger<HomeController> logger, INotyfService notyf)
        {
            _logger = logger;
            _notyf = notyf;
        }

        [HttpGet]
        public IActionResult Index(List<Account>? accounts = null)
        {
            accounts ??= new List<Account>();

            return View(accounts);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult Index(IFormFile file, [FromServices] IWebHostEnvironment hostingEnvironment)
        {
            if (file is null)
            {
                _notyf.Error("Please selcet a file to upload");
                return RedirectToAction("Index");
            }

            try
            {
                //Upload Csv
                string fileName = $"{hostingEnvironment.WebRootPath}//files//{file.FileName}";

                using (FileStream fileStream = System.IO.File.Create(fileName))
                {
                    file.CopyTo(fileStream);
                    fileStream.Flush();
                }

                var accounts = GetAccountList(file.FileName);
                return Index(accounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _notyf.Error("File is not valid, please check file type and try again");
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult Nuban()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Nuban(Bank bank)
        {
            if (ModelState.IsValid && bank.BankCode.Length == 6)
            {
                var bankCode = bank.BankCode;
                var serialNo = bank.SerialNo;

                ViewData["nuban"] = $"NUBAN: {GenerateCheckDigit(bankCode, serialNo)}";

            }
            else
            {
                TempData["info"] = "Bank code is 6 digit";
                _notyf.Error("Bank code is 6 digit");
                return RedirectToAction("Nuban", "Account");
            }

            return View();
        }
        private List<Account> GetAccountList(string fileName)
        {
            List<Account> accounts = new List<Account>();

            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    MissingFieldFound = null
                };

                //I changed "\" to "/" because i was getting file not found exception on linux container.
                //Read Csv
                var path = Directory.GetCurrentDirectory() + "/wwwroot/files//" + fileName;
                using (var reader = new StreamReader(path))
                using (var csv = new CsvReader(reader, config))
                {
                    csv.Read();
                    csv.ReadHeader();

                    while (csv.Read())
                    {
                        var account = csv.GetRecord<Account>();
                        accounts.Add(account);

                        var bankCode = "990046";
                        var SerialNumber = account.SerialNo;
                        var gen = GenerateCheckDigit(bankCode, SerialNumber);
                        account.NubanNo = gen;
                    }
                }

                //Create Csv

                path = Directory.GetCurrentDirectory() + "/wwwroot/filesTo";
                using (var write = new StreamWriter(path + "//NewFile.csv"))
                using (var csv = new CsvWriter(write, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(accounts);
                }
            }
            catch (BadDataException ex)
            {
                ModelState.AddModelError("", ex.Message);
                _logger.LogError(ex.Message);
                _notyf.Error("Something went wrong. Check uploaded file and fields");

            }

            return accounts;
        }

        private string GenerateCheckDigit(string bankCode, string serialNumber)
        {
            const int serialNumberLenght = 9;
            _ = bankCode + serialNumber;

            serialNumber = serialNumber.PadLeft(serialNumberLenght, '0');
            string cipher = bankCode + serialNumber;
            var nubanSum = 0;

            // Step 1. Calculate A*3+B*7+C*3+D*3+E*7+F*3+G*3+H*7+I*3+J*3+K*7+L*3

            var cipherArray = new int[cipher.Length];
            for (int i = 0; i < cipher.Length; i++)
            {
                cipherArray[i] = Convert.ToInt32(cipher[i].ToString());
            }
            if (bankCode.Length == 6)
            {
                nubanSum = (cipherArray[0] * 3) + (cipherArray[1] * 7) + (cipherArray[2] * 3) + (cipherArray[3] * 3) +
                         (cipherArray[4] * 7) + (cipherArray[5] * 3) + (cipherArray[6] * 3) + (cipherArray[7] * 7) +
                         (cipherArray[8] * 3) + (cipherArray[9] * 3) + (cipherArray[10] * 7) + (cipherArray[11] * 3) +
                         (cipherArray[12] * 3) + (cipherArray[13] * 7) + (cipherArray[14] * 3);
            }
            else if (bankCode.Length == 3)
            {
                nubanSum = (cipherArray[0] * 3) + (cipherArray[1] * 7) + (cipherArray[2] * 3) + (cipherArray[3] * 3) +
                         (cipherArray[4] * 7) + (cipherArray[5] * 3) + (cipherArray[6] * 3) + (cipherArray[7] * 7) +
                         (cipherArray[8] * 3) + (cipherArray[9] * 3) + (cipherArray[10] * 7) + (cipherArray[11] * 3);
            }

            // Step 2 & 3: Calculate Modulo 10 of your result i.e. the remainder after dividing by 10 then subtract 10
            var calCheckDigit = 10 - (nubanSum % 10);

            // Step 4: If your result is 10, then use 0 as your check digit
            calCheckDigit = calCheckDigit != 10 ? calCheckDigit : 0;
            return serialNumber + calCheckDigit.ToString().Normalize();
        }

        private bool ValidateNubanAccount(string bankCode, string accountNumber)
        {
            var result = false;
            try
            {
                if (bankCode.Trim().Length == 3 || bankCode.Trim().Length == 5 && accountNumber.Trim().Length == 10)
                {
                    int nubanSum = 0;
                    string nuban = bankCode + accountNumber.Remove(9, 1);
                    int checkDigit = Convert.ToInt32(accountNumber.Substring(9, 1));
                    int[] nubanArray = new int[nuban.Length];
                    for (int i = 0; i < nuban.Length; i++)
                    {
                        nubanArray[i] = Convert.ToInt32(nuban[i].ToString());
                    }

                    if (bankCode.Trim().Length == 5)
                    {
                        nubanSum = (nubanArray[0] * 3) + (nubanArray[1] * 7) + (nubanArray[2] * 3) + (nubanArray[3] * 3) +
                                   (nubanArray[4] * 7) + (nubanArray[5] * 3) + (nubanArray[6] * 3) + (nubanArray[7] * 7) +
                                   (nubanArray[8] * 3) + (nubanArray[9] * 3) + (nubanArray[10] * 7) + (nubanArray[11] * 3) +
                                   (nubanArray[12] * 3) + (nubanArray[13] * 7) + (nubanArray[14] * 3);
                    }
                    else if (bankCode.Trim().Length == 3)
                    {
                        nubanSum = (nubanArray[0] * 3) + (nubanArray[1] * 7) + (nubanArray[2] * 3) + (nubanArray[3] * 3) +
                                  (nubanArray[4] * 7) + (nubanArray[5] * 3) + (nubanArray[6] * 3) + (nubanArray[7] * 7) +
                                  (nubanArray[8] * 3) + (nubanArray[9] * 3) + (nubanArray[10] * 7) + (nubanArray[11] * 3);
                    }

                    int calCheckDigit = 10 - (nubanSum % 10);
                    calCheckDigit = calCheckDigit != 10 ? calCheckDigit : 0;
                    result = checkDigit == calCheckDigit;
                }
            }
            catch { }
            return result;
        }
    }
}
