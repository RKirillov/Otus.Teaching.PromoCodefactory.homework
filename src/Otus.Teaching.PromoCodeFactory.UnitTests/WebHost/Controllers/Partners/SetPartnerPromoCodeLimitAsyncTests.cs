﻿using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Otus.Teaching.PromoCodeFactory.Core.Abstractions.Repositories;
using Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement;
using Otus.Teaching.PromoCodeFactory.WebHost.Controllers;
using Otus.Teaching.PromoCodeFactory.WebHost.Models;
using Xunit;
using YamlDotNet.Core;

namespace Otus.Teaching.PromoCodeFactory.UnitTests.WebHost.Controllers.Partners
{
    public class SetPartnerPromoCodeLimitAsyncTests
    {
        private readonly Mock<IRepository<Partner>> _partnersRepositoryMock;
        private readonly PartnersController _partnersController;
        private readonly DateTime specificDateTime = new DateTime(2021, 5, 1, 21, 53, 30);

        public SetPartnerPromoCodeLimitAsyncTests()
        {
            //TODO не работает
            //Func<DateTime> getDateTime = () => DateTime.Now;

            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            //fixture.Inject(specificDateTime);
            fixture.Register<DateTime?>(() => specificDateTime);
            //This means that every time we request an instance of a frozen type, we will get the same instance. You can think of it as registering a singleton instance in an IoC container.
            _partnersRepositoryMock = fixture.Freeze<Mock<IRepository<Partner>>>();
            //Before creating the object, the Build method can be used to add one-time customizations to be used for the creation of the next variable.
            _partnersController = fixture.Build<PartnersController>()
                //The With construct allows the customization of writeable properties and public fields.
                .With(x => x.CurrentDateTime, () => specificDateTime)
                .OmitAutoProperties()
                .Create();
            //fixture.Register <DateTime>(() => specificDateTime);
        }

        /// SetPartnerPromoCodeLimitAsync

        [Fact]
        public async void SetPartnerPromoCodeLimit_PartnerIsNotFound_ReturnsNotFound()
        {
            // Arrange
            var partnerId = Guid.Parse("def47943-7aaf-44a1-ae21-05aa4948b165");
            Partner partner = null;
            var partnerLimitRequest = new SetPartnerPromoCodeLimitRequest();

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partnerId))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partnerId, partnerLimitRequest);

            // Assert
            result.Should().BeAssignableTo<NotFoundResult>();
        }

        //Если партнер заблокирован, то есть поле IsActive=false в классе Partner, то также нужно выдать ошибку 400;
        [Theory, AutoData]
        public async void SetPartnerPromoCodeLimit_PartnerIsNotActive_ReturnsBadRequest(SetPartnerPromoCodeLimitRequest partnerLimitRequest)
        {
            //var partnerId = Guid.Parse("def47943-7aaf-44a1-ae21-05aa4948b165");
            var partner = CreateBasePartner("def47943-7aaf-44a1-ae21-05aa4948b165");
            partner.IsActive = false;

            //спрятать в autofixture
            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, partnerLimitRequest);

            // Assert
            result.Should().BeAssignableTo<BadRequestObjectResult>();
        }

        //Если партнеру выставляется лимит, то мы должны обнулить количество промокодов, которые партнер выдал NumberIssuedPromoCodes,
        //При установке лимита нужно отключить предыдущий лимит
        //[Fact]
        [Theory, AutoData]
        public async void SetPartnerPromoCodeLimit_SetNewLimit_ReturnsZeroPromocodesAndCancelDate(SetPartnerPromoCodeLimitRequest partnerLimitRequest)
        {
            // Arrange
            var partner = CreateBasePartner("7d994823-8226-4273-b063-1a95f3cc1df8");
            partner.NumberIssuedPromoCodes = 100;
            //спрятать в autofixture
            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            var result = (await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, partnerLimitRequest) as CreatedAtActionResult).Value as Partner;

            // Assert
            result.Should().BeAssignableTo<Partner>();
            result.PartnerLimits.Select(x => x.CancelDate).Should().Contain(specificDateTime);
            result.NumberIssuedPromoCodes.Should().Be(0);

        }
        // если лимит закончился, то количество не обнуляется;
        [Theory, AutoData]
        public async void SetPartnerPromoCodeLimit_SetNewLimit_ReturnsNotZeroPromocodes(SetPartnerPromoCodeLimitRequest partnerLimitRequest)
        {
            // Arrange
            var partner = CreateBasePartner("7d994823-8226-4273-b063-1a95f3cc1df8", DateTime.UtcNow);

            var expectedPromocedes = partner.NumberIssuedPromoCodes = 100;
            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            var result = (await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, partnerLimitRequest) as CreatedAtActionResult).Value as Partner;

            // Assert
            result.Should().BeAssignableTo<Partner>();
            result.NumberIssuedPromoCodes.Should().Be(expectedPromocedes);
        }


        //Лимит должен быть больше 0;
        [Theory, AutoData]
        public async void SetPartnerPromoCodeLimit_SetNewLimitLessZero_ReturnsBadRequest(SetPartnerPromoCodeLimitRequest partnerLimitRequest)
        {
            // Arrange
            var partner = CreateBasePartner("7d994823-8226-4273-b063-1a95f3cc1df8");
            partnerLimitRequest.Limit = -1;
            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, partnerLimitRequest);

            //Assert
            result.Should().BeAssignableTo<BadRequestObjectResult>();
        }


        //Нужно убедиться, что сохранили новый лимит в базу данных (это нужно проверить Unit-тестом);
        [Theory, AutoData]
        public async void SetPartnerPromoCodeLimit_SetNewLimit_VerifyUpdatePartner(SetPartnerPromoCodeLimitRequest partnerLimitRequest)
        {
            // Arrange
            var partner = CreateBasePartner("7d994823-8226-4273-b063-1a95f3cc1df8");
            partnerLimitRequest.Limit = 100;
            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, partnerLimitRequest);

            // Assert
            _partnersRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Partner>()), Times.Once);
        }


        public Partner CreateBasePartner(string id = "def47943-7aaf-44a1-ae21-05aa4948b165", DateTime? cancelDate = null)
        {
            var partner = new Partner()
            {
                Id = Guid.Parse(id),
                Name = "Суперигрушки",
                IsActive = true,
                PartnerLimits = new List<PartnerPromoCodeLimit>()
                {
                    new PartnerPromoCodeLimit()
                    {
                        Id = Guid.Parse("e00633a5-978a-420e-a7d6-3e1dab116393"),
                        CreateDate = new DateTime(2020, 07, 9),
                        EndDate = new DateTime(2020, 10, 9),
                        Limit = 100,
                        CancelDate= cancelDate
                    }
                }
            };

            return partner;
        }
    }
}