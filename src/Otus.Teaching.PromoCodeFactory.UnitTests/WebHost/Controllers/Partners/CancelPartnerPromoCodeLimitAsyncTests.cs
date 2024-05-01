using System;
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


namespace Otus.Teaching.PromoCodeFactory.UnitTests.WebHost.Controllers.Partners
{
    public class CancelPartnerPromoCodeLimitAsyncTests
    {
        private readonly Mock<IRepository<Partner>> _partnersRepositoryMock;
        private readonly PartnersController _partnersController;
        //private readonly SetPartnerPromoCodeLimitRequest _partnerLimitRequest;
        //private readonly Mock<SetPartnerPromoCodeLimitRequest> _setPartnerPromoCodeLimitRequest;

        public CancelPartnerPromoCodeLimitAsyncTests()
        {
            //TODO не работает
            var specificDateTime = new DateTime(2021, 5, 1, 21, 53, 30);
            var fixture = new Fixture().Customize(new AutoMoqCustomization())
                .Customize(new CustomDateTimeCustomization(specificDateTime));
            fixture.Register<DateTime?>(() => specificDateTime);
            _partnersRepositoryMock = fixture.Freeze<Mock<IRepository<Partner>>>();
            _partnersController = fixture.Build<PartnersController>().OmitAutoProperties().Create();


            //var someEntity = new Fixture().Build<Entity>().With(e => e.Name, "Important For Test").Without(e => e.Group).Create();
            //_partnerLimitRequest = fixture.Build<SetPartnerPromoCodeLimitRequest>()
            //    .With(f => f.Limit, 10)
            //    .With(f => f.EndDate, DateTime.Now).Create();

        }

        public Partner CreateBasePartner(string id = "def47943-7aaf-44a1-ae21-05aa4948b165", DateTime? cancelDate=null)
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

        [Fact]
        public async void CancelPartnerPromoCodeLimit_PartnerIsNotFound_ReturnsNotFound()
        {
            // Arrange
            var partnerId = Guid.Parse("def47943-7aaf-44a1-ae21-05aa4948b165");
            Partner partner = null;

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partnerId))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.CancelPartnerPromoCodeLimitAsync(partnerId);

            // Assert
            result.Should().BeAssignableTo<NotFoundResult>();
        }

        [Fact]
        public async void CancelPartnerPromoCodeLimit_PartnerIsNotActive_ReturnsBadRequest()
        {
            // Arrange без разницы какой id устанавливать
            var partnerId = Guid.Parse("7d994823-8226-4273-b063-1a95f3cc1df8");
            //var partnerId=Guid.Empty;
            var partner = CreateBasePartner("7d994823-8226-4273-b063-1a95f3cc1df8");
            partner.IsActive = false;

            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partnerId))
                .ReturnsAsync(partner);

            // Act
            var result = await _partnersController.CancelPartnerPromoCodeLimitAsync(partnerId);

            // Assert
            result.Should().BeAssignableTo<BadRequestObjectResult>();
        }
        //закончили CancelPartnerPromoCodeLimitAsync
        //тестируем SetPartnerPromoCodeLimitAsync 

        //Также есть функционал работы с партнерам, для партнера устанавливаются лимиты на выдачу промокодов, если лимит превышен или закончился срок его действия, то промокод нельзя выдать
        //Если партнер не найден, то также нужно выдать ошибку 404;
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

        //
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
        //[Fact]
        [Theory, AutoData]
        public async void SetPartnerPromoCodeLimit_ResetPromocodes_ReturnsZeroCountAndCancelDate(SetPartnerPromoCodeLimitRequest partnerLimitRequest)
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
            result.PartnerLimits.Select(x => x.CancelDate).Should().Contain(It.IsNotNull<DateTime?>());
            result.NumberIssuedPromoCodes.Should().Be(0);
  
        }
        // если лимит закончился, то количество не обнуляется;
        [Theory, AutoData]
        public async void SetPartnerPromoCodeLimit_ResetPromocodes_ReturnsNotZeroCount(SetPartnerPromoCodeLimitRequest partnerLimitRequest)
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


        //При установке лимита нужно отключить предыдущий лимит;
        [Theory, AutoData]
        public async void SetPartnerPromoCodeLimit_ResetPrevLimit_ReturnsDataCancel(SetPartnerPromoCodeLimitRequest partnerLimitRequest)
        {
            // Arrange
            var partner = CreateBasePartner("7d994823-8226-4273-b063-1a95f3cc1df8");


            var expectedPromocedes = partner.NumberIssuedPromoCodes = 100;
            _partnersRepositoryMock.Setup(repo => repo.GetByIdAsync(partner.Id))
                .ReturnsAsync(partner);

            // Act
            var result = (await _partnersController.SetPartnerPromoCodeLimitAsync(partner.Id, partnerLimitRequest) as CreatedAtActionResult).Value as Partner;

        }
        //Лимит должен быть больше 0;

        //Нужно убедиться, что сохранили новый лимит в базу данных (это нужно проверить Unit-тестом);
        //Если в текущей реализации найдутся ошибки, то их нужно исправить и желательно написать тест, чтобы они больше не повторялись.
    }

    public class CustomDateTimeCustomization : ICustomization
    {
        private readonly DateTime _specificDateTime;

        public CustomDateTimeCustomization(DateTime specificDateTime)
        {
            _specificDateTime = specificDateTime;
        }

        public void Customize(IFixture fixture)
        {
            fixture.Register(() => _specificDateTime);
        }
    }
}