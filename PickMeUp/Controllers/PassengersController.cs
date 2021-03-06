﻿using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using PickMeUp.Entity;
using PickMeUp.Models.Passenger;
using PickMeUp.Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PickMeUp.Controllers
{
    
    public class PassengersController : Controller
    {
        private IPassengerRepository _passengerRepository;
        private IVehicleRepository _vehicleRepository;
        private IVehicleTypeRepository _vehicleTypeRepository;
        private IPaymentTypeRepository _paymentTypeRepository;
        private IPaymentRepository _paymentRepository;
        private IRideRepository _rideRepository;

        public PassengersController()
        {

        }

        public PassengersController(IPassengerRepository passengerRepository, IVehicleRepository vehicleRepository, IVehicleTypeRepository vehicleTypeRepository, IPaymentTypeRepository paymentTypeRepository, IPaymentRepository paymentRepository, IRideRepository rideRepository)
        {
            _passengerRepository = passengerRepository;
            _vehicleRepository = vehicleRepository;
            _vehicleTypeRepository = vehicleTypeRepository;
            _paymentTypeRepository = paymentTypeRepository;
            _paymentRepository = paymentRepository;
            _rideRepository = rideRepository;
        }





        // GET: Passengers
        public ActionResult Index()
        {
            //PassengersViewModel model = new PassengersViewModel();

            //User user = System.Web.HttpContext.Current.GetOwinContext()
            //            .GetUserManager<ApplicationUserManager>()
            //            .FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());

            ////Passenger passenger = _passengerRepository.GetPassengerByUser(user.Id);


            ////Ride ride = new Ride()
            ////{
            ////    Id = model.RideId,
            ////    StartLocation = model.StartLocation,
            ////    EndLocation = model.EndLoaction,
            ////    PassengerId = passenger.Id,
            ////    RideStatus = RideStatus.NotAccepted,
            ////};



            var VehicleTypesList = new List<SelectListItem>();
            var vehicleTypes = _vehicleTypeRepository.GetAll();

            foreach (var vT in vehicleTypes)
                VehicleTypesList.Add(new SelectListItem() { Value = vT.Name, Text = vT.Name });

            ViewBag.VehicleTypes = VehicleTypesList;
            

            var PaymentTypesList = new List<SelectListItem>();
            var paymentTypes = _paymentTypeRepository.GetAll();
            foreach (var pT in paymentTypes)
                PaymentTypesList.Add(new SelectListItem() { Value = pT.Name, Text = pT.Name });

            ViewBag.PaymentTypes = PaymentTypesList;

            User user = System.Web.HttpContext.Current.GetOwinContext()
                            .GetUserManager<ApplicationUserManager>()
                            .FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());

            Passenger passenger = _passengerRepository.GetPassengerByUser(user.Id);
            var ride = _rideRepository.Find(r => r.PassengerId == passenger.Id && (r.RideStatus == RideStatus.OnGoing || r.RideStatus == RideStatus.NotAccepted)).FirstOrDefault();
            if (ride != null)
                ViewBag.Msg = "Soon some of our best drivers will contact with you. Thank you for using our service.";


            return View(new PassengersViewModel());
        }

        [HttpPost]
        [ActionName("Index")]
        public ActionResult RequestRide(PassengersViewModel model)
        {
            if (ModelState.IsValid)
            {
                User user = System.Web.HttpContext.Current.GetOwinContext()
                            .GetUserManager<ApplicationUserManager>()
                            .FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());

                Passenger passenger = _passengerRepository.GetPassengerByUser(user.Id);


                Payment payment = new Payment()
                {
                    PassengerId = passenger.Id,
                    PaymentTypeId = _paymentTypeRepository.Find(p => p.Name == model.PaymentType).SingleOrDefault().Id,
                    
                };
                //will be changed in the future for Debit and Credit Cards
                if (!model.PaymentType.Equals("Cash"))
                {
                    payment.Payed = true;
                }

                payment.Amount = model.Distance * _vehicleTypeRepository.Find(p => p.Name == model.VehicleType).SingleOrDefault().FareRate;

                Ride ride = new Ride()
                {
                    StartLocation = model.StartLocation,
                    EndLocation = model.EndLocation,
                    PassengerId = passenger.Id,
                    RideStatus = RideStatus.NotAccepted,
                    Payment = payment,
                    VehicleTypeId = _vehicleTypeRepository.Find(v => v.Name == model.VehicleType).SingleOrDefault().Id
                };

                _paymentRepository.Add(payment);
                _rideRepository.Add(ride);

                return RedirectToAction("RideOnGoing");
            }

            else
            {
                var VehicleTypesList = new List<SelectListItem>();
                var vehicleTypes = _vehicleTypeRepository.GetAll();

                foreach (var vT in vehicleTypes)
                    VehicleTypesList.Add(new SelectListItem() { Value = vT.Name, Text = vT.Name });

                ViewBag.VehicleTypes = VehicleTypesList;


                var PaymentTypesList = new List<SelectListItem>();
                var paymentTypes = _paymentTypeRepository.GetAll();
                foreach (var pT in paymentTypes)
                    PaymentTypesList.Add(new SelectListItem() { Value = pT.Name, Text = pT.Name });

                string Error = "";
                
               //add model error here

                ViewBag.PaymentTypes = PaymentTypesList;
                ModelState.AddModelError("",  Error);
            }
            return View(model);
        }


        public ActionResult RideOnGoing()
        {
            ViewBag.Msg = null;
               User user = System.Web.HttpContext.Current.GetOwinContext()
                            .GetUserManager<ApplicationUserManager>()
                            .FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());

            Passenger passenger = _passengerRepository.GetPassengerByUser(user.Id);

            var ride = _rideRepository.Find(r => r.PassengerId == passenger.Id && (r.RideStatus == RideStatus.OnGoing || r.RideStatus == RideStatus.NotAccepted)).FirstOrDefault();
            if(ride== null)
                ViewBag.Msg = "Soon some of our best drivers will contact with you. Thank you for using our service.";


            return View(ride);
        }

        public ActionResult CancelRide(int? id)
        {
            var ride = _rideRepository.Get(id);
            ride.RideStatus = RideStatus.Cancelled;
            ride.StartTime = System.DateTime.Now;
            ride.EndTime = System.DateTime.Now;
            _rideRepository.Update(ride);

            return RedirectToAction("Index");

        }

        public ActionResult AllRides()
        {
            User user = System.Web.HttpContext.Current.GetOwinContext()
                            .GetUserManager<ApplicationUserManager>()
                            .FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());

            Passenger passenger = _passengerRepository.GetPassengerByUser(user.Id);
            var rides = _rideRepository.GetAllRidesForPassenger(passenger.Id);
            ViewBag.TotalAmount = 0;


            foreach(var r in rides)
            {
                if (r.RideStatus == RideStatus.Finished)
                    ViewBag.TotalAmount += r.Payment.Amount;
            }

            return View(rides);
        }


        [HttpPost]
        public JsonResult GetFareRate(string vT)
        {
            VehicleType vehicleType = _vehicleTypeRepository.GetVehicleByName(vT);
            return Json(vehicleType.FareRate, JsonRequestBehavior.AllowGet);
        }
    }
}