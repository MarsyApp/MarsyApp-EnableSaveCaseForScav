import {DependencyContainer} from "tsyringe";
import {ILogger} from "@spt-aki/models/spt/utils/ILogger";
import {ProfileHelper} from "@spt-aki/helpers/ProfileHelper";
import {IPmcData} from "@spt-aki/models/eft/common/IPmcData";
import {ItemHelper} from "@spt-aki/helpers/ItemHelper";
import {ISaveProgressRequestData} from "@spt-aki/models/eft/inRaid/ISaveProgressRequestData";
import {InraidCallbacks} from "@spt-aki/callbacks/InraidCallbacks";
import {PlayerRaidEndState} from "@spt-aki/models/enums/PlayerRaidEndState";
import {HashUtil} from "@spt-aki/utils/HashUtil";
import * as config from "../config/config.json";

class Mod {
	private static logger: ILogger;
	private static hashUtil: HashUtil;
	private static itemHelper: ItemHelper;
	private static secureContainerTemplate: string;

	preAkiLoad(container: DependencyContainer): void {
		Mod.logger = container.resolve<ILogger>("WinstonLogger");
		Mod.itemHelper = container.resolve<ItemHelper>("ItemHelper");
		Mod.hashUtil = container.resolve<HashUtil>("HashUtil");
		Mod.secureContainerTemplate = config.secureContainerTemplate;

		container.afterResolution("ProfileHelper", (_t, result: ProfileHelper) => {
			const oldRemoveSecureContainer = result.removeSecureContainer.bind(result);
			result.removeSecureContainer = (profile: IPmcData) => {
				const profileResult = oldRemoveSecureContainer(profile);
				const items = profileResult.Inventory.items;
				const defaultInventory = items.find((x) => x._tpl === "55d7217a4bdc2d86028b456d");
				const secureContainer = items.find((x) => x.slotId === "SecuredContainer");

				if (!secureContainer && defaultInventory) {
					profileResult.Inventory.items.push({
						"_id": Mod.hashUtil.generate(),
						"_tpl": Mod.secureContainerTemplate,
						"parentId": defaultInventory._id,
						"slotId": "SecuredContainer"
					});
				}

				return profileResult;
			}
		}, {frequency: "Always"});

		container.afterResolution("InraidCallbacks", (_t, result: InraidCallbacks) => {
			const oldSaveProgress = result.saveProgress.bind(result);
			result.saveProgress = (url: string, info: ISaveProgressRequestData, sessionID: string) => {

				const statusOnExit = info.exit;
				const isScav = info.isPlayerScav;
				const isDead = statusOnExit !== PlayerRaidEndState.SURVIVED && statusOnExit !== PlayerRaidEndState.RUNNER
				if (isScav && isDead) {
					const inventory = info.profile.Inventory;
					const items = inventory.items;
					const secureContainer = items.find((x) => x.slotId === "SecuredContainer");
					if (secureContainer) {
						const childItemsInSecureContainer = Mod.itemHelper.findAndReturnChildrenByItems(
							items,
							secureContainer._id
						);

						info.profile.Inventory.items = items.filter((x) => !x?.parentId || childItemsInSecureContainer.includes(x._id));
					}

					info.exit = PlayerRaidEndState.SURVIVED;
				}

				return oldSaveProgress(url, info, sessionID);
			}
		}, {frequency: "Always"});
	}
}

module.exports = {mod: new Mod()}
