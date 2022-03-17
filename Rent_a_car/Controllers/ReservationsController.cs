﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rent_a_car.Entities;
using Rent_a_car.ExtentionMethods;
using Rent_a_car.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rent_a_car.Controllers
{
    public class ReservationsController : Controller
    {
        private Rent_a_carDBContext _database;
        public ReservationsController(Rent_a_carDBContext database)
        {
            _database = database;
        }
        public ActionResult Index()
        {
            var user = HttpContext.Session.GetObject<Users>("loggedUser");
            List<Reservations> reservationHistory = new List<Reservations>();
            if (user.IsAdmin == 0)
                reservationHistory = _database.Reservations.Include(r => r.Car).Where(r => r.UserId == user.Id).ToList();
            else
                reservationHistory = _database.Reservations.Include(r => r.Car).ToList();
            ViewData["reservationHistory"] = reservationHistory;
            _database.Reservations.Where(r => r.Status == (int)Statuses.Aproved && r.DateOfReservation > DateTime.Now).ForEachAsync(r => r.Status = (int)Statuses.Ongoing);
            _database.Reservations.Where(r => r.Status == (int)Statuses.Ongoing && r.EndDate < DateTime.Now).ForEachAsync(r => r.Status = (int)Statuses.Completed);
            _database.Reservations.Where(r => r.EndDate < DateTime.Today).ForEachAsync(r => r.Status = (int)Statuses.Missed);
            _database.SaveChanges();
            return View();
        }

        [HttpPost]
        public ActionResult ApproveReservation(int id)
        {
            var reservation = _database.Reservations.Where(r => r.Id == id).FirstOrDefault();
            reservation.Status = (int)Statuses.Aproved;
            reservation.AprovedDate = DateTime.Now;
            _database.SaveChanges();
            return RedirectToAction("Index");
        }
        [HttpPost]
        public ActionResult RejectReservation(int id)
        {
            var reservation = _database.Reservations.Where(r => r.Id == id).FirstOrDefault();
            reservation.Status = (int)Statuses.Upcoming;
            reservation.AprovedDate = null;
            _database.SaveChanges();
            return RedirectToAction("Index");
        }
        public ActionResult MakeReservations()
        {
            List<SelectListItem> cars = new List<SelectListItem>();
            var avalableCars = _database.Cars.Include(c => c.Reservations)
                .Where
                (c => c.Reservations
                        .Where(r => r.Status == (int)Statuses.Completed || 
                               r.Status == (int)Statuses.Missed)
                        .Count() > 0 ||
                        c.Reservations.Count() == 0
                ).ToList();
            foreach (var car in avalableCars)
            {
                cars.Add(new SelectListItem(string.Format($"{car.Brand} {car.Model} {car.Year}"), car.Id.ToString()));
            }
            ViewBag.AvalableCarsIDsList = cars;
            return View();
        }
        [HttpPost]
        public ActionResult MakeReservations(ReservationsVM input)
        {
            if (!this.ModelState.IsValid)
                return View(input);
            var user = HttpContext.Session.GetObject<Users>("loggedUser");
            var reservation = input.GetReservation();
            reservation.Status = (int)Statuses.Upcoming;
            reservation.UserId = user.Id;
            _database.Reservations.Add(reservation);
            _database.SaveChanges();
            return Index();
        }
    }
}